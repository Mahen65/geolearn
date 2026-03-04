# 22 — Shapefile Tool: drag to move + export filename input

## What
- Re-enabled **drag to move** on drawn shapes (was lost when the Leaflet.Draw edit toolbar was removed)
- Added an **Export filename** text input — both download buttons use this name for the zip file

## Files changed
- `src/shapefile-tool/templates/index.html`

## Drag
`leaflet-path-transform` patches `L.Path.prototype.makeDraggable()` when loaded. `attachTransform()` now calls it before enabling rotation:
```js
layer.makeDraggable();
layer.dragging.enable();
layer.on('dragend', () => { currentGeometry = layer.toGeoJSON().geometry; ... });
```
`clearSelection()` also calls `activeLayer.dragging.disable()` for clean teardown.

## Filename input
`getExportName()` reads `#exportName`, strips path separators, collapses spaces to underscores, falls back to `my_area`.
- Clip download → `<name>_buildings.zip`
- Export polygon download → `<name>.zip`
