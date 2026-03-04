# 19 — Shapefile Tool: Docker integration

## What
Added `shapefile-tool` as a Docker Compose service so it runs alongside the rest of the stack.

## Files changed
- `src/shapefile-tool/Dockerfile` — new; `python:3.12-slim` image
- `src/shapefile-tool/app.py` — `host="0.0.0.0"` added so Flask binds on all interfaces inside the container; `FLASK_DEBUG` env var controls debug mode
- `docker-compose.yml` — new `shapefile-tool` service

## Docker Compose service
```yaml
shapefile-tool:
  build:
    context: src/shapefile-tool
  ports:
    - "5001:5001"
  volumes:
    - ./hotosm_lka_buildings_polygons_shp.zip:/data/source.zip:ro
  environment:
    SOURCE_SHAPEFILE: /data/source.zip
    FLASK_DEBUG: "0"
```

The source shapefile is bind-mounted read-only at `/data/source.zip`; `SOURCE_SHAPEFILE` points the app at it.

## To run
```bash
docker compose up shapefile-tool -d
# → http://localhost:5001
```
