# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ForestLink** is a geospatial data management platform for Swedish forestry operations. It ingests shapefiles from Swedish forestry clients (who use different GIS software with inconsistent attribute naming), stores geometry in PostGIS, and serves GeoJSON to a frontend map (Leaflet/MapLibre).

**Backend:** .NET with **NetTopologySuite** (NTS) for shapefile parsing and **Npgsql** for PostGIS integration. NTS + Npgsql allows writing geometry directly to PostGIS columns without manual conversion.

**Database:** PostgreSQL + PostGIS. The primary table is `workobjects`.

**Frontend:** React with Leaflet or MapLibre for map rendering.

**Data formats:** shapefiles in (input) → PostGIS `geometry` columns (storage) → GeoJSON (API output).

## Coordinate Systems

Three EPSG codes are used throughout the system:

| EPSG | Name | Unit | Role in ForestLink |
|------|------|------|--------------------|
| `4326` | WGS 84 | Degrees | API output, all GeoJSON responses, frontend maps |
| `3006` | SWEREF99 TM | Meters | **Database storage**, area/distance calculations |
| `3021` | RT90 2.5 gon V | Meters | Legacy import only — never write to RT90 |

**The fundamental pattern — memorize this:**
```sql
-- Store: incoming CRS → SWEREF99 TM in the DB
ST_Transform(incoming_geom, 3006)

-- Serve: SWEREF99 TM → WGS 84 GeoJSON for the API
ST_AsGeoJSON(ST_Transform(geom, 4326))
```

Geometry columns are typed as `geometry(Polygon, 3006)` — all shapes stored in SWEREF99 TM. The WGS 84 conversion happens only at the API boundary.

**SWEREF99 regional variants:** Sweden also has SWEREF99 12 00, SWEREF99 14 15, etc. — zone-specific variants that are more precise for specific longitude bands. You'll encounter these in incoming shapefiles. Each maps to its own EPSG code; the `.prj` file identifies which variant was used so PostGIS can transform correctly.

**`geometry` vs `geography`:** Use `geometry` columns (flat-plane, fast, full function support) since data is stored in SWEREF99 (already projected). `geography` works on a sphere and gives accurate real-world distances but is slower and supports fewer operations — do not use it for this project.

## Architecture & Data Flow

Shapefile upload → PostGIS storage → GeoJSON API response:

1. **Client uploads `.zip`** — shapefile bundle (SWEREF99 TM or RT90)
2. **Unpack & validate** — minimum required: `.shp`, `.shx`, `.dbf`; if `.prj` missing, default to SWEREF99 TM or reject and ask user to specify
3. **Read CRS from `.prj`** — detect EPSG; SWEREF99 TM (3006) is the reasonable default for Swedish forestry data
4. **Attribute normalisation** — apply per-client JSON mapping config (e.g., `AREAL → area_ha`)
5. **`ST_MakeValid` + `ST_Transform(geom, 3006)`** — repair then store in SWEREF99 TM
6. **Area queries** — `ST_Area(geom) / 10000.0` gives hectares (m² native in SWEREF99 TM)
7. **(Optional) GeoJSON preview** — before committing, convert to GeoJSON and return to the frontend for user confirmation; catches wrong CRS, mangled geometry, and wrong files early
8. **API response** — `ST_AsGeoJSON(ST_Transform(geom, 4326))` — WGS 84 GeoJSON
9. **Frontend renders** — React + Leaflet/MapLibre consume GeoJSON natively; style by status, attach click/hover interactions

**Work objects (parcels)** are the central entity. Parcels contain compartments; compartments contain harvesting blocks.

## Attribute Normalisation

Different Swedish forestry software exports shapefiles with different column names for the same data. ForestLink uses a **per-client JSON mapping config** to normalise:

| Client A column | Client B column | ForestLink internal name |
|-----------------|-----------------|--------------------------|
| `AREAL`         | `area_ha`       | `area_ha`                |
| `AVDNR`         | `Avdelning`     | `compartment_id`         |
| `TRAD`          | `tradslag`      | `species_code`           |
| `ALDER`         | `Alder_ar`      | `age_years`              |

This configurable mapping layer is a primary unimplemented feature. Normalisation happens after unpacking and before geometry transformation.

## PostGIS Functions

| Function | Use |
|----------|-----|
| `ST_Transform(g, srid)` | Reproject geometry |
| `ST_Area(g)` | Area in m² (divide by 10,000 for hectares) |
| `ST_IsValid(g)` | Check if geometry is valid (use before MakeValid) |
| `ST_Intersects(a, b)` | Spatial overlap check (any shared space) |
| `ST_Within(a, b)` | True if A is completely inside B |
| `ST_AsGeoJSON(g)` | Serialize to GeoJSON string |
| `ST_GeomFromText(wkt, srid)` | Parse WKT into geometry |
| `ST_MakeValid(g)` | Repair self-intersections, open rings |
| `ST_SRID(g)` | Return the geometry's SRID |
| `ST_MakeEnvelope(xmin, ymin, xmax, ymax, srid)` | Create bounding box for spatial filters |

All geometry columns need a **GiST spatial index** — standard B-tree indexes don't work for geometry. Without one, every spatial query does a full table scan.

```sql
CREATE INDEX idx_workobjects_geom ON workobjects USING GIST (geom);
```

## Critical Gotchas

- **Always transform before serving.** Returning SWEREF99 TM coordinates in a GeoJSON response places polygons thousands of km from Sweden. Every SELECT returning geometry must use `ST_AsGeoJSON(ST_Transform(geom, 4326))`.
- **GeoJSON coordinate order is `[longitude, latitude]`**, not `[latitude, longitude]`. Stockholm at lon=18, lat=59 written as `[59, 18]` ends up in Africa.
- **Always set SRID on insert.** `ST_GeomFromText('...', 0)` is accepted but breaks `ST_Transform`. Use the correct EPSG code.
- **Always run `ST_MakeValid` before inserting.** Client shapefiles frequently contain self-intersections. Skip this and downstream area calculations return NULL.
- **Swedish characters (å, ä, ö)** in `.dbf` attribute tables are often Windows-1252 encoded. Check `.cpg` file; detect encoding if absent.
- **Reject uploads without `.prj`.** Without a known CRS, the geometry cannot be safely transformed.
- **RT90 coordinates are meter-scale but different from SWEREF99 TM.** Inserting RT90 data as SRID 3006 silently places features hundreds of km off.
- **Mixed geometry types.** A shapefile should contain only one type, but you'll occasionally get `MultiPolygon` mixed with `Polygon`. Insert logic must handle both.
- **Large shapefiles.** Detailed forestry boundaries can be sizable. Stream the NTS reader rather than loading all features into memory at once.

## Reading Shapefiles in .NET

Use `NetTopologySuite.IO.ShapefileDataReader` — it yields geometry and attributes in one pass:

```csharp
using NetTopologySuite.IO;

var reader = new ShapefileDataReader("workobject.shp", new GeometryFactory());
while (reader.Read())
{
    var geometry = reader.Geometry;      // NTS geometry object
    var attributes = reader.GetValues(); // attribute row
    // validate → transform → map to schema → insert via Npgsql
}
```

Npgsql with the NetTopologySuite plugin writes NTS geometry objects directly to PostGIS `geometry` columns — no manual WKT/WKB conversion needed.

## Sample Data

`/sample_forest_parcels/` contains a reference shapefile (`.shp`, `.shx`, `.dbf`, `.prj`, `.cpg`) for testing the import pipeline.
