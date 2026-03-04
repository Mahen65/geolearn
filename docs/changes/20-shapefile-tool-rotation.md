# 20 — Shapefile Tool: shape rotation + transform handles

## What
Added `leaflet-path-transform` plugin so drawn shapes can be rotated, scaled, and dragged after drawing — without needing to redraw.

## Files changed
- `src/shapefile-tool/templates/index.html`
  - Added `leaflet-path-transform` CSS + JS from unpkg CDN
  - Removed Leaflet.Draw edit toolbar (transform replaces it)
  - `L.Draw.Event.CREATED` handler now calls `layer.transform.enable({ rotation: true, scaling: true })`
  - `transformed` event listener syncs `currentGeometry` after each move/rotate/scale
  - `clearSelection()` calls `activeLayer.transform.disable()` before removing the layer

## How it works
After drawing a shape, three handle types appear:
| Handle | Location | Action |
|---|---|---|
| Circle | Above the shape (top) | Drag to **rotate** |
| Squares | Four corners | Drag to **scale** |
| Shape body | Anywhere on the polygon | Drag to **move** |

The `transformed` event fires on mouse-up and updates `currentGeometry` so all API calls (clip, export, count) use the latest rotated/scaled coordinates.
