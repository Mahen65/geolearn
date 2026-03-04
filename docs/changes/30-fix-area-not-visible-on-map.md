# 30 — Fix: Imported area not visible on map

## Root causes

1. **No fitBounds** — after loading features, the map never zoomed to them.
   Building footprints are tiny polygons; at the default Sri Lanka view (zoom 8) they
   are invisible specks. The user had to manually zoom/pan to find them.

2. **No auto-load after upload** — after a successful shapefile import the user had to
   manually select a country in WsmFilterPanel and click Load. This was non-obvious.

3. **WsmFilterPanel country list stale** — `fetchCountries()` runs on mount. If no
   work objects existed yet, the country dropdown was empty and could never be populated
   without refreshing the page.

4. **`DetectSrid` missed WKT2 format** — `geopandas >= 1.0` with `pyproj 3.x` writes
   the `.prj` file with WKT2 text `GEOGCS["WGS 84",...]` and the authority tag
   `ID["EPSG",4326]`. Neither matched the existing checks (`GCS_WGS_1984`,
   `WGS 1984`, `WGS_1984`), so the code fell through to the default SRID 3006
   (SWEREF99 TM — Sweden), placing Sri Lanka buildings thousands of km away.

## What changed

### `src/GeoLearn.Web/src/components/ParcelLayer.jsx`
- Imported `useMap` from `react-leaflet`
- Added `useEffect([dataKey])` that calls `map.fitBounds()` on the GeoJSON layer
  every time a new load starts — the map flies to wherever the loaded features are

### `src/GeoLearn.Web/src/components/UploadButton.jsx`
- `onUploadSuccess()` → `onUploadSuccess(countryCode)` — passes the selected country
  code up so App can auto-load

### `src/GeoLearn.Web/src/App.jsx`
- Added `uploadVersion` and `uploadedCountry` state
- `handleDataChanged(countryCode)` now increments `uploadVersion`, stores
  `uploadedCountry`, and immediately calls `handleLoadWsm` for the uploaded country
- Passes `uploadVersion` and `uploadedCountry` to `WsmFilterPanel`

### `src/GeoLearn.Web/src/components/WsmFilterPanel.jsx`
- Accepts `uploadVersion` and `uploadedCountry` props
- `fetchCountries` useEffect now depends on `uploadVersion` — re-fetches the country
  list after each upload so newly-imported countries appear in the dropdown
- New `useEffect([uploadVersion, uploadedCountry])` selects the uploaded country and
  resets main/sub category filters

### `src/GeoLearn.Api/Services/ShapefileImportService.cs` — `DetectSrid`
- Added two extra WGS 84 patterns to match geopandas/pyproj WKT2 output:
  - `"WGS 84"` (quoted, WKT2 geographic CRS name)
  - `EPSG",4326` (authority tag in WKT2: `ID["EPSG",4326]`)
