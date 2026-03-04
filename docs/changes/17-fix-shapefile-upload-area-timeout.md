# 17 — Fix Shapefile Upload Area Timeout

## Problem
Shapefile upload returned HTTP 500. The 6 features from the sample shapefile were inserted successfully, but the post-insert step:
```sql
UPDATE work_objects SET area_ha = ST_Area(geom) / 10000.0 WHERE area_ha IS NULL
```
ran over the entire table — 3,704,000 rows with `area_ha IS NULL` (the NSDI-imported features). This exceeded EF Core's 30 s command timeout, causing `Npgsql.NpgsqlException: Timeout during reading attempt`.

## Root Cause
`NsdiImportService` imports NSDI features without computing `area_ha`, leaving it NULL for all 3.7M NSDI rows. The post-import UPDATE had always scanned all NULL rows regardless of which upload just happened.

## Fix — `Services/ShapefileImportService.cs`

**Compute area inline in the INSERT** instead of a separate UPDATE pass:

```sql
-- Before
INSERT INTO work_objects (name, area_ha, ...)
SELECT @Name, @AreaHa, ...

-- After (area_ha computed by PostGIS when shapefile doesn't provide it)
INSERT INTO work_objects (name, area_ha, ...)
SELECT
    @Name,
    COALESCE(@AreaHa::double precision,
             ST_Area(ST_Transform(ST_SetSRID(ST_GeomFromWKB(@GeomWkb), {sourceSrid}), 5234)) / 10000.0),
    ...
```

**Removed** the post-insert `ExecuteSqlRawAsync("UPDATE work_objects SET area_ha = ...")` entirely — no more full-table scan.

## Result
- Upload completes in ~1 s for small shapefiles
- `area_ha` is always populated on INSERT (PostGIS computes it when the shapefile doesn't supply it)
- No more timeout regardless of how many NSDI rows exist in the database
