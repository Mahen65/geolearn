# 10 — Sri Lanka NSDI Forest Boundary Import

## What changed
Added the ability to import 10,252 forest boundary polygons from the Sri Lanka
National Spatial Data Infrastructure (NSDI) ArcGIS REST API directly into PostGIS.
Storage CRS changed from SWEREF99 TM (EPSG:3006) to Sri Lanka Grid 1999 (EPSG:5234).

## Source
- Service: https://nsdi.gov.lk/spatial-data-services
- Layer: Forest Boundary (layer 1) in the Boundary MapServer
- REST API: https://gisapps.nsdi.gov.lk/server/rest/services/SLNSDI/Boundary/MapServer/1
- Features: 10,252 polygons | maxRecordCount: 1,000 | CRS: WGS 84 (EPSG:4326)

## Files modified / created

| File | Action |
|------|--------|
| `src/GeoLearn.Api/Data/AppDbContext.cs` | Changed geometry column type to `geometry(Geometry,5234)` |
| `src/GeoLearn.Api/Migrations/20260226081800_ChangeSridTo5234.cs` | New — raw SQL migration |
| `src/GeoLearn.Api/Services/NsdiImportService.cs` | New — REST API pagination + PostGIS transform |
| `src/GeoLearn.Api/Services/ShapefileImportService.cs` | Updated transform target 3006 → 5234 |
| `src/GeoLearn.Api/Controllers/WorkObjectsController.cs` | Added `POST /workobjects/import-nsdi` |
| `src/GeoLearn.Api/Program.cs` | Registered `HttpClient<NsdiImportService>` (5 min timeout) |
| `src/GeoLearn.Web/src/api.js` | Added `importNsdiData()` |
| `src/GeoLearn.Web/src/components/NsdiImportButton.jsx` | New component |
| `src/GeoLearn.Web/src/App.jsx` | Added `NsdiImportButton` |

## CRS change: 3006 → 5234

| CRS | Name | Used for |
|-----|------|---------|
| EPSG:4326 | WGS 84 | API output, all GeoJSON responses |
| EPSG:5234 | Sri Lanka Grid 1999 | **Database storage**, area/distance calculations |
| EPSG:4326 | WGS 84 | Source (NSDI input) |

The pattern is identical to the original Swedish design:
- incoming data (WGS 84) → transform on INSERT → stored in projected CRS
- `ST_Area(geom) / 10000` → accurate hectares in projected metres
- `ST_Transform(geom, 4326)` at the API boundary → WGS 84 GeoJSON

## Import pipeline (NsdiImportService)

```
HTTP GET /query?where=1=1&resultOffset=0&resultRecordCount=1000&f=geojson
  → parse features[] from GeoJSON response
  → for each feature:
      INSERT ... ST_Transform(ST_SetSRID(ST_GeomFromGeoJSON(@geomJson), 4326), 5234)
  → commit transaction
→ repeat with offset += 1000 until features.Count < 1000
→ UPDATE work_objects SET area_ha = ST_Area(geom)/10000 WHERE area_ha IS NULL
```

## NSDI field mapping

| NSDI field | WorkObject field | Notes |
|---|---|---|
| `forest_name` | `name` | Falls back to `"<description> — <district>"` |
| `area_final` | `area_ha` | Already in hectares in the source |
| `division` | `compartment_id` | Forest administrative division |
| `description` | `species_code` | e.g. "Dense Forests", "Open Forests" |

## Migration note
The `ChangeSridTo5234` migration **truncates** `work_objects` before changing the
column type. Any previously imported Swedish shapefile data will be lost. Re-import
via the shapefile upload endpoint if needed.
