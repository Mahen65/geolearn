# 01 — Docker PostGIS Setup

## What
Added `docker-compose.yml` at the repo root to run a local PostGIS database for development.

## Files
- `docker-compose.yml` — new

## Details
- Image: `postgis/postgis:17-3.5` (PostgreSQL 17 + PostGIS 3.5)
- Database: `forestlink` / user: `forestlink` / password: `forestlink`
- Port: `5432` (standard Postgres port)
- Data persisted in a named Docker volume `postgis_data`

## Commands
```bash
# Start PostGIS in the background
docker compose up -d

# Check it's running
docker compose ps

# Stop
docker compose down

# Stop and wipe data (reset)
docker compose down -v
```

## Why PostGIS?
Plain PostgreSQL has no concept of geometry. PostGIS adds:
- `geometry` column type (stores points, lines, polygons)
- Spatial functions (`ST_Transform`, `ST_Area`, `ST_AsGeoJSON`, etc.)
- GiST spatial indexing

The `postgis/postgis` image ships with `CREATE EXTENSION postgis` already applied to the default database, so no manual setup is needed.
