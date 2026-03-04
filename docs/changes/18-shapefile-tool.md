# 18 — Shapefile Tool (Map-based Area Selector)

## What
Added `src/shapefile-tool/` — a standalone Python/Flask web app that lets you draw an area on a Leaflet map and generate shapefiles from it.

## Why
The full HOT OSM Sri Lanka buildings shapefile (255 MB, 3.7 M features) is too large to upload directly. This tool lets you clip out just the area you need.

## Files added
- `src/shapefile-tool/app.py` — Flask backend with three endpoints
- `src/shapefile-tool/templates/index.html` — Leaflet map UI with draw tools
- `src/shapefile-tool/requirements.txt` — flask, geopandas, shapely

## Endpoints
| Route | Method | Description |
|---|---|---|
| `/` | GET | Leaflet map UI |
| `/feature-count` | POST | Returns count of buildings in drawn area (fast bbox pre-check) |
| `/clip` | POST | Clips source shapefile to drawn shape, returns `.zip` download |
| `/export-polygon` | POST | Exports the drawn polygon itself as a shapefile `.zip` |

## Key design decision
`gpd.read_file(SOURCE_ZIP, bbox=bbox)` passes the bounding box to GDAL as a spatial filter — only features within the bbox are read from disk. This avoids loading all 3.7 M features into memory and makes even large files respond quickly for small selections.

## How to run
```bash
cd src/shapefile-tool
pip install -r requirements.txt
python app.py        # starts at http://localhost:5001
```

Set `SOURCE_SHAPEFILE` env var to override the default source zip path.
