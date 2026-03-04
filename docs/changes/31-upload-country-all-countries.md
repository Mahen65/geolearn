# 31 — Fix: Upload country dropdown always showed Sri Lanka

## Root cause

`UploadButton` used `fetchCountries()` → `GET /workobjects/countries`, which only
returns countries that already have at least one work object. With only LK data in
the DB, the dropdown was locked to Sri Lanka regardless of what the user intended.

## What changed

### `src/GeoLearn.Api/Controllers/CountriesController.cs` (new)
New controller with a single `GET /countries` endpoint that returns **all** countries
from the reference table, ordered by name.

### `src/GeoLearn.Web/src/api.js`
Added `fetchAllCountries()` → `GET /countries`.

### `src/GeoLearn.Web/src/components/UploadButton.jsx`
Switched from `fetchCountries` to `fetchAllCountries` so all 20 reference countries
are always available in the upload country selector.
