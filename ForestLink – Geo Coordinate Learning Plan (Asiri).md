# ForestLink — Geo Coordinate System Learning Plan

*A structured, self-paced guide for Asiri*
*Based on: ForestLink Technical Primer & Client Meeting with Erik*

---

## About This Plan

This plan takes you from zero to confident with the geospatial concepts used every day inside ForestLink. It is built directly from the **ForestLink Technical Primer** and the **recorded client meeting with Erik**, so everything you learn here connects immediately to the codebase and the customer conversations you will join.

The plan is split into five phases. Each phase builds on the previous one and ends with a concrete exercise so you can test your understanding before moving on. Expect to spend roughly **15–20 hours** in total, spread across your first two to three weeks on the project.

| Phase | Title | Time Est. | Focus |
|-------|-------|-----------|-------|
| 1 | Foundations of Geographic Coordinates | 2–3 hrs | Concepts |
| 2 | The Three Coordinate Systems in ForestLink | 3–4 hrs | Theory + SQL |
| 3 | PostGIS – The Spatial Database Layer | 4–5 hrs | Hands-on SQL |
| 4 | Spatial Data Formats: Shapefiles & GeoJSON | 3–4 hrs | Formats + Code |
| 5 | ForestLink Architecture & Client Context | 2–3 hrs | Integration |

---

## Phase 1 — Foundations of Geographic Coordinates

*Understanding why location on Earth needs a mathematical framework*

| | |
|---|---|
| **Estimated Time** | 2–3 hours |
| **Difficulty** | Beginner – no prior GIS knowledge needed |
| **Key Outcome** | You can explain why coordinate systems exist and what EPSG codes mean |

### What You Will Learn

**The Earth is not flat.**
The Earth is an irregular sphere (an ellipsoid). Mapping it onto flat screens or paper always involves distortion. Different coordinate systems make different trade-offs between accuracy, area, and shape. Every distortion decision ForestLink makes — storage vs. display — traces back to this physical reality.

**Latitude & Longitude.**
Latitude measures degrees north/south from the equator (0°–±90°). Longitude measures degrees east/west from the Greenwich meridian (0°–±180°). Together they pin any point on Earth. This is the native language of GPS devices and the web. You will see these numbers in every GeoJSON file ForestLink serves.

**Degrees vs. Meters.**
Degrees are angular units — useful globally, but 1 degree of longitude near Sweden (≈57°N) is only ~62 km wide. Meters are constant everywhere. When ForestLink calculates forest area, it must use a meter-based system. This is the core reason ForestLink stores geometry in SWEREF99 TM (meters) but publishes it in WGS 84 (degrees).

**EPSG Codes.**
Every coordinate system has a numeric ID from the EPSG registry. EPSG:4326 = WGS 84 (degrees). EPSG:3006 = SWEREF99 TM (meters for Sweden). These codes are what you pass to PostGIS's `ST_Transform` function. You will see EPSG codes everywhere in the codebase — in SQL queries, in `.prj` files inside shapefiles, and in API responses.

### Resources

- **What is a Coordinate System?** — [esri.com/arcgis-blog](https://www.esri.com/arcgis-blog/products/arcgis-pro/mapping/coordinate-systems-difference/) *(Clear explainer, no software needed)*
- **EPSG.io Interactive Map** — [epsg.io](https://epsg.io) *(Type any EPSG code and visualise the projection)*
- **Latitude/Longitude explainer (video)** — [YouTube](https://www.youtube.com/watch?v=swKBi0hV9L0) *(10-minute visual introduction)*

### Phase 1 Exercise

Open [epsg.io](https://epsg.io) and look up both **EPSG:4326** and **EPSG:3006**. For each one, note down: (a) the unit of measurement, (b) the area of the world it covers, and (c) what the bounding box coordinates look like. Then answer: **why can't ForestLink just use WGS 84 for everything?**

Write your answers in a short note — a paragraph is enough. Share it with a colleague to check your understanding before moving to Phase 2.

---

## Phase 2 — The Three Coordinate Systems in ForestLink

*WGS 84, SWEREF99 TM and the legacy RT90 — what each does and when you'll meet it*

| | |
|---|---|
| **Estimated Time** | 3–4 hours |
| **Difficulty** | Beginner to Intermediate |
| **Key Outcome** | You can explain the role of each CRS and write the SQL to convert between them |

### The Three Systems at a Glance

| System | EPSG Code | Unit | Coverage | Where in ForestLink |
|--------|-----------|------|----------|---------------------|
| **WGS 84** | 4326 | Degrees | Global | API output, GeoJSON, frontend maps |
| **SWEREF99 TM** | 3006 | Meters | Sweden | Database storage, area calculations |
| **RT90** | 3021 | Meters | Sweden (old) | Legacy data imports only |

### Deep Dive: WGS 84 (EPSG:4326)

WGS 84 (World Geodetic System 1984) is the coordinate system your phone's GPS uses. Coordinates are expressed as decimal degrees of longitude and latitude. Stockholm is roughly at longitude 18.07, latitude 59.33.

ForestLink uses WGS 84 as the **output format** for its API and all GeoJSON responses. The frontend mapping libraries (Leaflet and MapLibre) also expect WGS 84 by default. Think of it as the "shipping format" for spatial data.

> **Critical gotcha:** In GeoJSON the coordinate order is `[longitude, latitude]` — NOT `[latitude, longitude]` as most people expect. Getting this wrong silently places features in the ocean.

### Deep Dive: SWEREF99 TM (EPSG:3006)

SWEREF99 TM is the official Swedish national coordinate system, adopted by Lantmäteriet. Coordinates are in meters. The origin is far south-west of Sweden so all Swedish coordinates are large positive numbers (Northing ~6,100,000–7,700,000 m; Easting ~270,000–920,000 m).

ForestLink stores **all geometry in the database** using SWEREF99 TM. This is deliberate: meter-based systems let PostGIS calculate areas (`ST_Area`) and distances (`ST_Distance`) accurately without any conversion overhead at query time. The conversion to WGS 84 only happens at the API boundary.

### Deep Dive: RT90 (EPSG:3021)

RT90 is the predecessor to SWEREF99 TM. Many older Swedish datasets and historical forest inventories still use it. ForestLink must be able to import RT90 shapefiles and convert them on the way in. You will not write to RT90 — you only read it.

### The Conversion Pattern (ST_Transform)

PostGIS converts between any two EPSG codes with a single function call. The pattern you will see throughout the ForestLink codebase is:

```sql
-- Store incoming WGS 84 geometry as SWEREF99 TM
INSERT INTO parcels (geom) VALUES (
  ST_Transform(ST_GeomFromText('POINT(18.07 59.33)', 4326), 3006)
);

-- Serve geometry back to the frontend as WGS 84 GeoJSON
SELECT ST_AsGeoJSON(ST_Transform(geom, 4326)) FROM parcels;
```

### Resources

- **SWEREF99 official documentation** — [lantmateriet.se](https://www.lantmateriet.se/en/geodata/gps-geodesi-och-swepos/referenssystem/tvadimensionella-system/sweref-99-tm/) *(Swedish mapping authority)*
- **PostGIS ST_Transform reference** — [postgis.net](https://postgis.net/docs/ST_Transform.html) *(Official function documentation)*
- **Proj.org CRS database** — [proj.org](https://proj.org/en/stable/) *(The underlying library PostGIS uses)*

### Phase 2 Exercise

Using a local PostgreSQL/PostGIS instance (or the ForestLink dev environment), run the two SQL statements from the code block above. Then: (1) verify the stored SWEREF99 TM coordinates are large meter-scale numbers, and (2) confirm that `ST_AsGeoJSON` returns degrees. If you don't have a database yet, work through the SQL statements on paper and explain to yourself what each argument means.

---

## Phase 3 — PostGIS: The Spatial Database Layer

*How PostgreSQL grows a brain for geography*

| | |
|---|---|
| **Estimated Time** | 4–5 hours |
| **Difficulty** | Intermediate – SQL experience helpful |
| **Key Outcome** | You can query spatial data, perform area calculations, check intersections, and fix invalid geometries |

### What PostGIS Adds to PostgreSQL

PostgreSQL is a powerful relational database but it has no native understanding of points, lines, or polygons. PostGIS is an extension that adds: `geometry` and `geography` column types, 300+ spatial functions (`ST_*`), spatial indexing (GiST), and knowledge of all EPSG coordinate systems.

When ForestLink runs `CREATE EXTENSION postgis;` in its database migration, it unlocks all of this. Every forest parcel, every compartment boundary, every harvesting block is stored as a geometry column.

### Core Concepts

**`geometry` type.**
A column of type `geometry` stores the actual shape: point, linestring, polygon, or multi-variants. You specify the SRID when you create or insert the column so PostGIS always knows the coordinate system. ForestLink's parcels table uses `geometry(Polygon, 3006)` — a polygon stored in SWEREF99 TM.

**SRID (Spatial Reference ID).**
The SRID is the EPSG code embedded in a geometry. `ST_SRID(geom)` returns it. If you insert geometry without the right SRID, conversions silently produce wrong coordinates. Always verify the SRID matches the data source. A shapefile from a client in RT90 inserted as 3006 will be plotted hundreds of kilometres away.

**`ST_MakeValid`.**
Spatial data from external sources often contains self-intersections, duplicate vertices, or open rings that violate the geometry rules PostGIS enforces. `ST_MakeValid` repairs most of these automatically. Shapefiles uploaded by forestry clients will sometimes be invalid. ForestLink runs `ST_MakeValid` before inserting to prevent silent failures downstream.

**GiST Spatial Index.**
A standard B-tree index on a geometry column is useless — you can't compare shapes with `<`. GiST (Generalized Search Tree) indexes bounding boxes. Spatial queries on large tables are 100× faster with it. ForestLink creates a GiST index on every geometry column.

### The Functions You Will Use Every Day

| Function | What it does | ForestLink use case |
|----------|-------------|---------------------|
| `ST_Transform(geom, srid)` | Reprojects geometry to a new coordinate system | Convert stored 3006 → 4326 for API output |
| `ST_Area(geom)` | Returns area in the geometry's native units (m² for SWEREF99 TM) | Calculate parcel area in hectares |
| `ST_Intersects(a, b)` | Returns true if two geometries share any space | Find which parcels overlap a given boundary |
| `ST_AsGeoJSON(geom)` | Serialises geometry to a GeoJSON string | Build API responses for the frontend |
| `ST_GeomFromText(wkt, srid)` | Parses WKT text into a geometry value | Insert geometries from text or test fixtures |
| `ST_MakeValid(geom)` | Repairs invalid geometry | Sanitise uploaded shapefiles before storage |
| `ST_SRID(geom)` | Returns the SRID of a geometry | Verify incoming data has the expected CRS |

### Resources

- **PostGIS official documentation** — [postgis.net/docs](https://postgis.net/docs/manual-3.4/) *(The reference manual – bookmark this)*
- **PostGIS intro tutorial (Boundless)** — [postgis.net/workshops](https://postgis.net/workshops/postgis-intro/) *(Hands-on workshop, free online)*
- **pgAdmin 4 (GUI for PostGIS)** — [pgadmin.org](https://www.pgadmin.org/) *(Visualise query results in a map panel)*

### Phase 3 Exercise

Write a SQL query against the ForestLink dev database that does all three of the following in a single `SELECT`:

1. Returns each parcel's ID and its area in hectares (remember: `ST_Area` returns m², 1 hectare = 10,000 m²)
2. Transforms the geometry to WGS 84 and serialises it as GeoJSON
3. Filters to only parcels that intersect a bounding box you define

If you get stuck on the bounding box, search for `ST_MakeEnvelope` in the PostGIS docs — it is the function you need.

---

## Phase 4 — Spatial Data Formats: Shapefiles & GeoJSON

*The two formats ForestLink ingests and emits*

| | |
|---|---|
| **Estimated Time** | 3–4 hours |
| **Difficulty** | Intermediate |
| **Key Outcome** | You understand the structure of both formats, the common pitfalls, and how ForestLink's upload flow handles them |

### Shapefiles — The Industry Standard Input Format

Shapefiles are the most widely used format for exchanging GIS data in Sweden's forestry sector. Swedish municipalities and forestry companies produce them in SWEREF99 TM. A shapefile is always a **bundle of files** that must travel together as a `.zip`:

- **`.shp`** — the actual geometry (points, lines, or polygons)
- **`.shx`** — the geometry index (allows random access into .shp)
- **`.dbf`** — the attribute table (columns with names, areas, species codes, etc.)
- **`.prj`** — the CRS definition in WKT format — ForestLink reads this to know the EPSG code
- **`.cpg`** — *(optional)* character encoding declaration — critical for Swedish characters (å, ä, ö)

ForestLink's upload flow is: **accept .zip → unpack → verify** that .shp, .shx, .dbf, and .prj are all present → **read CRS** from .prj → **normalise attribute names** → `ST_MakeValid` → `ST_Transform` to SWEREF99 TM → **insert**.

### The Attribute Normalisation Challenge (from the Erik Meeting)

This topic came directly from the client meeting with Erik and is one of the most practically important things to understand. Different Swedish forestry organisations export shapefiles with **different column names** for the same data:

| Organisation A | Organisation B | ForestLink internal name |
|----------------|----------------|--------------------------|
| `AREAL` | `area_ha` | `area_ha` |
| `AVDNR` | `Avdelning` | `compartment_id` |
| `TRAD` | `tradslag` | `species_code` |
| `ALDER` | `Alder_ar` | `age_years` |

Erik confirmed that ForestLink needs a **configurable mapping layer**: a per-client JSON config that says "column AREAL maps to area_ha". This feature is not yet fully implemented. As you onboard you will work on this, so understanding the problem deeply is essential.

### Common Shapefile Pitfalls

- **Encoding issues:** Swedish å, ä, ö in `.dbf` files are often encoded in Windows-1252, not UTF-8. Without a `.cpg` file, ForestLink must detect the encoding or risk corrupted attribute values.
- **Invalid geometries:** Self-intersecting polygons, unclosed rings, and duplicate vertices all appear in real-world shapefiles. Always run `ST_MakeValid` before inserting.
- **Missing `.prj`:** Some tools omit the `.prj` file. ForestLink should reject the upload or ask the user to specify the CRS manually.
- **Mixed geometry types:** A single `.shp` can contain both polygons and multi-polygons. PostGIS handles this, but it must be anticipated in the schema.

### GeoJSON — The API Output Format

GeoJSON is a JSON-based format for representing geographic features. It is the format ForestLink uses for **all API responses** that include spatial data. The frontend libraries (Leaflet, MapLibre) consume GeoJSON natively.

```json
{
  "type": "Feature",
  "geometry": {
    "type": "Polygon",
    "coordinates": [[[18.07, 59.33], [18.08, 59.33], ...]]
    // ^^^ Always [longitude, latitude] order in WGS 84!
  }
}
```

> **Remember:** GeoJSON coordinates are ALWAYS in WGS 84 (EPSG:4326). This is part of the GeoJSON specification (RFC 7946), not a ForestLink choice.

### Resources

- **Shapefile format technical description** — [ESRI whitepaper (PDF)](https://www.esri.com/content/dam/esrisites/sitecore-archive/Files/Pdfs/library/whitepapers/pdfs/shapefile.pdf)
- **GeoJSON RFC 7946 specification** — [datatracker.ietf.org](https://datatracker.ietf.org/doc/html/rfc7946) *(The authoritative format specification)*
- **geojson.io** — [geojson.io](https://geojson.io) *(Paste or draw GeoJSON and visualise it instantly)*

### Phase 4 Exercise

Download a small Swedish forestry shapefile from the Swedish Forest Agency (Skogsstyrelsen) open data portal. Inspect its `.prj` file to confirm the CRS. Open the `.dbf` in LibreOffice Calc to see the attribute column names. Then write the attribute mapping config (a simple JSON object) that would map those column names to ForestLink's internal schema. Finally, copy one polygon's WKT geometry into [geojson.io](https://geojson.io) to visualise it.

---

## Phase 5 — ForestLink Architecture & Client Context

*Connecting everything you've learned to the actual system and the Erik meeting*

| | |
|---|---|
| **Estimated Time** | 2–3 hours |
| **Difficulty** | Intermediate – review the Technical Primer alongside this section |
| **Key Outcome** | You can trace a shapefile all the way from client upload to frontend map tile and explain every coordinate transformation along the route |

### The End-to-End Data Flow in ForestLink

Here is the complete journey of spatial data through ForestLink, with the coordinate system at each stage:

| # | Stage | CRS at this stage | Notes |
|---|-------|-------------------|-------|
| 1 | Client uploads `.zip` shapefile | SWEREF99 TM (usually) | Could also be RT90 for old data – read `.prj` to confirm |
| 2 | Unpack & validate | As received | Check all required files present; `ST_MakeValid` |
| 3 | Attribute normalisation | As received | Apply per-client column mapping config (Erik's feature) |
| 4 | Transform & store in PostGIS | **SWEREF99 TM (3006)** | `ST_Transform` to 3006 if source differs; insert with SRID 3006 |
| 5 | Backend query for area | **SWEREF99 TM (3006)** | `ST_Area` returns m²; divide by 10,000 for hectares |
| 6 | Backend query for API response | **WGS 84 (4326)** | `ST_AsGeoJSON(ST_Transform(geom, 4326))` |
| 7 | Frontend map render | **WGS 84 (4326)** | Leaflet/MapLibre consume the GeoJSON directly |

### Key Takeaways from the Erik Meeting

The meeting with Erik surfaced several important context points that give the technical work its purpose:

- ForestLink is used by Swedish forestry operators who manage multiple forest properties. **Work objects (parcels) are the central entity.**
- Clients export their own GIS data from different forestry management software, resulting in inconsistent attribute naming — the normalisation challenge discussed above.
- Erik confirmed that **SWEREF99 TM is the expected default CRS** for Swedish clients. RT90 data only appears in legacy scenarios.
- The **preview step** (loading a shapefile into GeoJSON before committing to the database) is valued by Erik because it lets the client confirm the data looks right before it becomes permanent.

### Common Mistakes to Avoid on the Team

**Forgetting to Transform Before Serving.**
Serving SWEREF99 TM coordinates in a GeoJSON response. The frontend will silently place polygons thousands of kilometres from Sweden. Always wrap with `ST_AsGeoJSON(ST_Transform(geom, 4326))` in every SELECT that returns geometry.

**Confusing Lat/Lon Order in GeoJSON.**
Putting `[latitude, longitude]` in a GeoJSON coordinates array. GeoJSON spec requires `[longitude, latitude]`. A polygon near Stockholm at lon=18, lat=59 placed as `[59, 18]` ends up in the middle of Africa. When in doubt: longitude first, latitude second — same as X, Y on a standard map.

**Not Running `ST_MakeValid` on Upload.**
Inserting a shapefile polygon with a self-intersection directly into PostGIS. Downstream area calculations return NULL or wrong values. Always run `ST_MakeValid` as part of the import pipeline, before any other spatial operation.

**Inserting Without Setting SRID.**
Using `ST_GeomFromText('POLYGON(...)', 0)` with SRID 0 (unknown). PostGIS will accept it but `ST_Transform` will fail silently or throw an error. Always set the correct SRID when constructing or importing geometry.

### Phase 5 Exercise (Capstone)

Trace through the ForestLink codebase (backend + database layer) and find the exact lines of code that implement **steps 4, 5, and 6** from the end-to-end flow table above. For each step, write one sentence explaining what the code does and note the EPSG code it works with. Share your findings in the team channel.

If you cannot access the codebase yet, write a pseudocode version of all seven steps using only the PostGIS functions you learned in Phase 3.

---

## Quick Reference Card

*Keep this page handy during your first few weeks.*

### EPSG Codes You Will Use

| EPSG | Name | Unit | Use in ForestLink |
|------|------|------|-------------------|
| `4326` | WGS 84 | Degrees | API output, GeoJSON, GPS |
| `3006` | SWEREF99 TM | Meters | Database storage, area calculations |
| `3021` | RT90 2.5 gon V | Meters | Legacy import only |

### The Key PostGIS Functions

| Function | What it does |
|----------|-------------|
| `ST_Transform(g, srid)` | Reproject geometry to new CRS |
| `ST_Area(g)` | Area in native units (m² if SWEREF99 TM) |
| `ST_Intersects(a, b)` | True if geometries share any space |
| `ST_AsGeoJSON(g)` | Serialize to GeoJSON string |
| `ST_GeomFromText(wkt, srid)` | Parse WKT text into geometry |
| `ST_MakeValid(g)` | Repair invalid geometry |
| `ST_SRID(g)` | Return SRID of geometry |

### The One Pattern to Memorise

```sql
-- Store: incoming CRS → SWEREF99 TM in the DB
ST_Transform(incoming_geom, 3006)

-- Serve: SWEREF99 TM → WGS 84 GeoJSON for the API
ST_AsGeoJSON(ST_Transform(geom, 4326))
```

---

*Good luck, Asiri — you've got this!*
