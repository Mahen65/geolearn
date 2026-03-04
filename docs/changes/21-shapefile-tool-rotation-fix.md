# 21 — Shapefile Tool: fix rotation (bundle plugin locally)

## Root cause
`leaflet-path-transform@0.1.4` CDN URLs (both JS and CSS) return 404 — the package version doesn't exist on unpkg.

Additionally, v1.9.0 of the plugin only attaches `.transform` to a layer via `addInitHook` **if the layer was created with `{ transform: true }` in its options**. Leaflet.Draw creates layers without this option, so `layer.transform` was always `undefined`.

## Fix
1. Downloaded `leaflet-path-transform@1.9.0` npm tarball, extracted `dist/index.js` → `src/shapefile-tool/static/leaflet-path-transform.js`. Flask serves it at `/static/leaflet-path-transform.js` — no CDN dependency.
2. Removed the broken CSS `<link>` (v1.9.0 has no separate CSS file).
3. Replaced `if (activeLayer.transform)` guard with `attachTransform(layer)` which manually instantiates `new window.PathTransform.Transform(layer, { rotation: true, scaling: true })` and calls `.enable()` — works regardless of how the layer was created.

## Files changed
- `src/shapefile-tool/static/leaflet-path-transform.js` — new (bundled from npm, 20 KB)
- `src/shapefile-tool/templates/index.html` — script tag points to `/static/`, removed CSS link, manual `attachTransform()` function
