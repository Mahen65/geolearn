# 11 — Fix large shapefile upload ("Failed to fetch")

## Problem

Uploading `hotosm_lka_buildings_polygons_shp.zip` (255 MB) always failed with a
browser "Failed to fetch" error and no useful error message.

Two separate issues:

1. **`MultipartBodyLengthLimit` (128 MB default) exceeded.**
   ASP.NET Core's multipart form reader has its own body-size limit, separate from
   Kestrel's `MaxRequestBodySize`. The existing `[DisableRequestSizeLimit]` attribute
   only removes the Kestrel limit. For a 255 MB file the multipart reader threw an
   exception mid-stream, causing Kestrel to reset the TCP connection before it could
   send an HTTP response — which the browser reports as "Failed to fetch" instead of a
   clean error status.

2. **No fetch timeout on the upload call.**
   Unlike `importNsdiData`, `uploadShapefile` had no `AbortController` timeout.
   Large shapefiles with many features can take 10+ minutes to process server-side.

## Fix

### `WorkObjectsController.cs`

Added `[RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]` alongside the
existing `[DisableRequestSizeLimit]` on the `Upload` action.

### `api.js`

Added a 10-minute `AbortController` timeout to `uploadShapefile`, matching the
pattern already used in `importNsdiData`.

## Files changed

- `src/GeoLearn.Api/Controllers/WorkObjectsController.cs`
- `src/GeoLearn.Web/src/api.js`
