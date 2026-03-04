# 09 — React + Vite Frontend (GeoLearn.Web)

## What changed
Added a React frontend at `src/GeoLearn.Web/` that renders forest parcels
on an interactive Leaflet map, shows PostGIS-computed attributes in a
sidebar on click, and supports shapefile upload.

## Files created / modified

| File | Action |
|------|--------|
| `src/GeoLearn.Web/` | New — Vite React scaffold |
| `src/GeoLearn.Web/Dockerfile` | New |
| `src/GeoLearn.Web/.dockerignore` | New |
| `src/GeoLearn.Web/.env.development` | New |
| `src/GeoLearn.Web/vite.config.js` | Modified — added `server.host` |
| `src/GeoLearn.Web/src/api.js` | New |
| `src/GeoLearn.Web/src/App.jsx` | Replaced scaffold |
| `src/GeoLearn.Web/src/index.css` | Replaced with height reset |
| `src/GeoLearn.Web/src/components/MapView.jsx` | New |
| `src/GeoLearn.Web/src/components/ParcelLayer.jsx` | New |
| `src/GeoLearn.Web/src/components/ParcelSidebar.jsx` | New |
| `src/GeoLearn.Web/src/components/UploadButton.jsx` | New |
| `docker-compose.yml` | Added `web` service |

## Architecture

```
App.jsx                  ← state owner: featureCollection, selectedParcel
├── MapView.jsx          ← MapContainer + TileLayer (OpenStreetMap)
│   └── ParcelLayer.jsx  ← <GeoJSON> polygons
├── ParcelSidebar.jsx    ← detail panel (right side)
└── UploadButton.jsx     ← floats bottom-left, calls /workobjects/upload
```

## Key patterns explained

### `key={JSON.stringify(data)}` on `<GeoJSON>`
react-leaflet's `GeoJSON` component does not re-render when the `data` prop
changes — Leaflet owns the underlying DOM node and won't update it through
React's reconciler. Changing `key` forces React to unmount the old component
and mount a fresh one, which redraws all polygons when parcels are refreshed.

### `server.host: '0.0.0.0'` in vite.config.js
Vite defaults to binding only on `127.0.0.1` (loopback). Inside Docker the
container's own loopback is separate from the host loopback, so the port
mapping (`5173:5173`) would never reach Vite. `0.0.0.0` makes Vite listen
on all interfaces, including the one Docker exposes to the host.

### `CHOKIDAR_USEPOLLING=true` in docker-compose.yml
Vite's file watcher uses inotify by default. Windows/WSL2 mounts don't
propagate inotify events to containers, so file changes never trigger hot
reload. Polling forces Chokidar to check for changes on a timer instead.

### AreaHa vs AreaHaLive
The sidebar displays both values side-by-side:
- `AreaHa` — the area value stored as an attribute in the original shapefile
- `AreaHaLive` — computed by PostGIS in real time: `ST_Area(geom) / 10000`

These should match (or be very close). If they differ it indicates a CRS
mismatch on import or a geometry repair that changed the shape.

## Running
```bash
docker compose up --build
# API:      http://localhost:8080
# Frontend: http://localhost:5173
```
