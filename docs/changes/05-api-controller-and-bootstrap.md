# 05 — API Controller & App Bootstrap

## What
Implemented `WorkObjectsController` with three endpoints and wired up `Program.cs` with EF Core + NTS, connection string, and auto-migration on startup.

## Files
- `src/ForestLink.Api/Controllers/WorkObjectsController.cs` — new
- `src/ForestLink.Api/Program.cs` — updated
- `src/ForestLink.Api/appsettings.json` — updated

---

## Endpoints

### `POST /workobjects/upload`
Accepts a multipart `.zip` file and runs the shapefile import pipeline.

```
curl -X POST https://localhost:PORT/workobjects/upload \
     -F "file=@sample_forest_parcels.zip"
```

Response:
```json
{ "inserted": 5, "message": "5 work object(s) imported successfully." }
```

### `GET /workobjects`
Returns all work objects as a **GeoJSON FeatureCollection**.

The raw SQL run against PostGIS:
```sql
SELECT
    id, name, area_ha, compartment_id, species_code, age_years, source_srid,
    ST_AsGeoJSON(ST_Transform(geom, 4326)) AS geojson   -- SWEREF99 TM → WGS 84
FROM work_objects
```

`ST_AsGeoJSON` produces a GeoJSON geometry string with coordinates in **WGS 84 (EPSG:4326)** —
`[longitude, latitude]` order. This is what Leaflet/MapLibre expect.

### `GET /workobjects/{id}`
Returns a single feature. Also shows `areaHaLive` computed on-the-fly:
```sql
SELECT ST_Area(geom) / 10000.0 FROM work_objects WHERE id = {id}
```
`ST_Area` returns m² in SWEREF99 TM (meter-based CRS). Dividing by 10,000 gives hectares.

---

## Why Raw SQL for GeoJSON?

The controller uses `db.Database.SqlQueryRaw<T>()` rather than EF LINQ.
This is intentional for learning: the `ST_AsGeoJSON(ST_Transform(geom, 4326))`
call is visible in the code, making it clear:
1. That a CRS transform happens on every read
2. That PostGIS serialises to GeoJSON, not .NET code

---

## Program.cs Key Parts

```csharp
// UseNetTopologySuite() — enables NTS Geometry ↔ PostGIS binary mapping.
// Without this, geometry columns return raw bytes.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsql => npgsql.UseNetTopologySuite()
    )
);
```

Auto-migration on startup (learning convenience — not for production):
```csharp
using var scope = app.Services.CreateScope();
scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
```

---

## appsettings.json Connection String

```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=forestlink;Username=forestlink;Password=forestlink"
}
```
Matches the Docker Compose credentials from change doc `01`.

EF Core SQL logging is enabled (`"Microsoft.EntityFrameworkCore.Database.Command": "Information"`)
so every SQL query including `ST_Transform` and `ST_AsGeoJSON` appears in the console output.
