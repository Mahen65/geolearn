# 32 — Fix: Uploaded work objects always saved with country LK

## Root cause

The shapefile tool's country dropdown stored only `lat,lng,zoom` in the option value.
When exporting a clip the country code was never embedded in the filename. The web app
upload button defaulted to `LK` and had no way to detect which country the clip was for.

## What changed

### `src/shapefile-tool/templates/index.html`
- Option value format changed from `lat,lng,zoom` → `code:lat,lng,zoom`
- `focusCountry()` updated to parse the new format
- Added `getCountryCode()` helper that reads the selected code
- Exported filenames now include the country code:
  - Clip: `{name}_{CC}_buildings.zip`
  - Export polygon: `{name}_{CC}.zip`

### `src/GeoLearn.Web/src/components/UploadButton.jsx`
- Before uploading, parses the country code from the filename
  (pattern: `_{CC}_buildings.zip` or `_{CC}.zip`)
- If a known country code is found it is used for the upload and
  the country dropdown is updated to reflect it
- Falls back to the manually selected country if no code is detected
