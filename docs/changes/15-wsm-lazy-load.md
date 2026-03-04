# 15 — WSM Lazy Loading (500-row pages)

## Problem
Loading all filtered results at once could still crash the browser for broad filters.

## Solution
Paginated loading — 500 rows per request. Subsequent pages append directly to the existing Leaflet layer via `addData()` without remounting or re-drawing existing features.

## Files changed

### Backend
- `Controllers/WorkObjectsController.cs` — added `offset` query param; SQL now includes `ORDER BY id LIMIT 500 OFFSET {offset}`; response includes `count`, `hasMore`, `nextOffset`

### Frontend
- `src/api.js` — `fetchAllParcels(filters, offset)` passes `offset` query param
- `src/components/ParcelLayer.jsx` — added `useRef` on the `<GeoJSON>` layer; `useEffect` watches `appendBatch` prop and calls `geoJsonRef.current.addData(batch)` for each new page
- `src/components/MapView.jsx` — passes `appendBatch` prop through to `ParcelLayer`
- `src/components/WsmFilterPanel.jsx` — added `Load 500 More` button (shown when `hasMore`); shows running feature count with "more available" hint
- `src/App.jsx` — tracks `nextOffset`, `hasMore`, `currentFilters`, `appendBatch`, `loadingMore`; `handleLoadMore` fetches next page and sets `appendBatch`

## Behaviour
- **Load WSM** → fetches offset=0, full remount of layer
- **Load 500 More** → fetches next page, appends via `addData()`, no remount
- Button disappears when `hasMore = false` (last page reached)
- Feature count increments with each page: `1,500 features loaded — more available`
