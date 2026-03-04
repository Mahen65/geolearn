# 16 — WSM NDJSON Streaming

## Problem
Paginated loading required 743+ HTTP round trips (at chunk size 500) to load the full 3.7M NSDI dataset. Each round trip added latency overhead.

## Solution
Single HTTP request using NDJSON streaming — the API streams one GeoJSON Feature per line as it reads from PostgreSQL. The frontend consumes the stream progressively, batching features and rendering them on the map without waiting for the full response.

## Files changed

### Backend
- `Controllers/WorkObjectsController.cs`
  - `GetAll` return type changed from `Task<IActionResult>` to `Task` (direct response writing)
  - Response content type: `application/x-ndjson; charset=utf-8`
  - `X-Accel-Buffering: no` header disables nginx/proxy buffering
  - Uses `AsAsyncEnumerable()` — EF Core reads rows from PostgreSQL one-by-one via `DbDataReader.ReadAsync()`
  - Each row serialized as `{"type":"Feature","geometry":{raw geojson},"properties":{...}}` + newline
  - Flushes to client every 200 rows
  - `OperationCanceledException` caught silently (expected on client disconnect)
  - Removed: `offset`, `pageSize` query params (pagination no longer needed)

### Frontend
- `src/api.js`
  - Removed `fetchAllParcels`
  - Added `streamParcels(filters, onBatch, signal)` — reads `response.body` as a `ReadableStream`, splits on newlines, parses each line as a Feature, calls `onBatch({ type, features })` every 500 features
  - Accepts `AbortSignal` for cancellation
- `src/App.jsx`
  - Removed pagination state (`chunkSize`, `currentFilters`, `hasMore`, `nextOffset`)
  - Added `abortRef` (`useRef`) — holds the current `AbortController`
  - `handleLoadWsm` creates a new `AbortController`, calls `streamParcels`, mounts layer on first batch, appends on subsequent batches
  - `handleUnloadWsm` calls `abortRef.current?.abort()` to cancel mid-stream
- `src/components/WsmFilterPanel.jsx`
  - Removed Chunk Size input and related props

## Result
- 1 HTTP request instead of 743
- Features appear on map within ~1 second of clicking Load WSM
- Button shows live count: `Loading… (500)` → `Loading… (1,000)` → …
- Unload cancels the stream immediately via AbortController
