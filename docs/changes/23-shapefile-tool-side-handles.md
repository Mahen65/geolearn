# 23 — Shapefile Tool: N/S/E/W side handles for axis-only scaling

## What
Added 4 midpoint handles at the edges of the bounding box for independent horizontal and vertical scaling, on top of the existing 4 corner handles from `leaflet-path-transform`.

## Handle legend
| Handle | Colour | Direction | Cursor |
|---|---|---|---|
| N (top mid) | light blue | vertical (anchored at S) | ns-resize |
| S (bottom mid) | light blue | vertical (anchored at N) | ns-resize |
| E (right mid) | light green | horizontal (anchored at W) | ew-resize |
| W (left mid) | light green | horizontal (anchored at E) | ew-resize |

## Implementation
- `sideHandlePos(layer, dir)` — computes the map position from `layer.getBounds()` midpoints
- `applyAxisScale(latlngs, origBounds, dir, handleLatLng)` — stretches all latlngs along one axis anchored at the opposite edge; handles nested arrays (polygon rings)
- `attachSideHandles(layer)` — creates 4 `L.CircleMarker` handles, enables drag via `makeDraggable()` (patched by leaflet-path-transform), captures `origBounds` + `origLatLngs` on mousedown, applies axis scale on drag, calls `layer.transform.reset()` to reposition corner handles
- `updateSideHandlePositions(layer)` — called after every `transformed` and `dragend` event to keep side handles in sync
- `removeSideHandles()` — called by `clearSelection()`

## Files changed
- `src/shapefile-tool/templates/index.html`
