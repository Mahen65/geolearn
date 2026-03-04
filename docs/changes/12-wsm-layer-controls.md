# 12 — WSM Layer Load/Unload Controls

## What changed

The workobject spatial map (WSM) parcel layer was previously auto-loaded on app startup. It is now off by default, controlled by explicit Load/Unload buttons.

## Files modified

- `src/GeoLearn.Web/src/App.jsx` — removed `useEffect` auto-fetch; added `handleLoadWsm` / `handleUnloadWsm` handlers and `wsmLoading` state; renders `<LayerControls>`
- `src/GeoLearn.Web/src/components/LayerControls.jsx` — new component; displays a "LAYERS" panel (top-right, z-index 1000) with Load WSM and Unload WSM buttons

## Behaviour

| State | Load WSM button | Unload WSM button |
|-------|-----------------|-------------------|
| Layer not loaded | Active (green) | Disabled |
| Loading in progress | Disabled ("Loading…") | Disabled |
| Layer loaded | Disabled | Active (red) |

Clicking **Load WSM** fetches `/workobjects` and renders the GeoJSON parcel layer.
Clicking **Unload WSM** clears `featureCollection` and deselects any open parcel.

## No data-flow changes

The upload and NSDI import buttons still call `loadParcels()` on success — this silently refreshes the layer without going through the WSM buttons.
