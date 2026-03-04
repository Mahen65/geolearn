# 04 — Shapefile Import Service

## What
Implemented `ShapefileImportService` — the core of the learning exercise. Ingests a shapefile `.zip`, parses it with NetTopologySuite, repairs invalid geometries, maps attributes to the internal schema, and stores everything in PostGIS.

## File
- `src/ForestLink.Api/Services/ShapefileImportService.cs` — new

## Pipeline (step by step)

```
.zip upload
    │
    ▼
1. Extract to temp dir  (System.IO.Compression.ZipArchive)
    │
    ▼
2. Detect source SRID   (read .prj WKT → EPSG code, default 3006)
    │
    ▼
3. Read features        (NetTopologySuite.IO.ShapefileDataReader)
    │
    ├─ Repair geometry  (geom.IsValid ? geom : geom.Buffer(0))
    │                    ↑ NTS equivalent of PostGIS ST_MakeValid
    ├─ Tag SRID         (geom.SRID = sourceSrid)
    │                    ↑ required for ST_Transform to work in PostGIS
    └─ Map attributes   (column name dict → WorkObject properties)
    │
    ▼
4. EF Core SaveChangesAsync  (Npgsql writes NTS geometry → PostGIS binary)
    │
    ▼
5. ST_Transform via raw SQL  (if source was not already 3006)
   UPDATE work_objects SET geom = ST_Transform(geom, 3006)
   WHERE source_srid <> 3006
    │
    ▼
6. Compute area_ha      (ST_Area(geom) / 10000.0 — accurate because geom is now in meters)
   UPDATE work_objects SET area_ha = ST_Area(geom) / 10000.0
   WHERE area_ha IS NULL
```

## Key Concepts Demonstrated

### NTS geometry repair
```csharp
var geom = rawGeom.IsValid ? rawGeom : rawGeom.Buffer(0);
```
`Buffer(0)` is the NTS idiom for repairing self-intersecting polygons.
Equivalent to `ST_MakeValid(geom)` in PostGIS SQL.

### SRID tagging before insert
```csharp
geom.SRID = sourceSrid;
```
If you insert geometry without a tagged SRID, PostGIS stores it with SRID=0 (unknown).
`ST_Transform` then fails because PostGIS doesn't know the source coordinate system.

### ST_Transform via raw SQL (intentional)
The transform is done as an explicit SQL UPDATE after insert, not in C# code.
This is intentional — it makes the coordinate transform visible and teaches the PostGIS pattern
`ST_Transform(geom, target_srid)` directly.

### Attribute mapping dictionary
```csharp
["AREAL"] = nameof(WorkObject.AreaHa),
["area_ha"] = nameof(WorkObject.AreaHa),
```
Case-insensitive dictionary maps Swedish forestry column naming variants to the
ForestLink internal schema. This is the configurable mapping layer described in the
technical primer.

## SRID Detection Logic
```
.prj contains "SWEREF99_TM" → EPSG:3006
.prj contains "RT90"        → EPSG:3021
.prj contains "WGS_1984"    → EPSG:4326
.prj missing               → default EPSG:3006
```
