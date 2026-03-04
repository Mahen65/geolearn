# 13 — pgAdmin in Docker Compose

## What changed

Added `pgadmin` service to `docker-compose.yml` for browser-based PostgreSQL administration.

## Files modified

- `docker-compose.yml` — added `pgadmin` service and `pgadmin_data` volume

## Access

| URL | http://localhost:5050 |
|-----|-----------------------|
| Email | `admin@geolearn.local` |
| Password | `admin` |

## Connecting to the database in pgAdmin

When adding a server in pgAdmin use these connection details:

| Field | Value |
|-------|-------|
| Host | `postgis` |
| Port | `5432` |
| Database | `geolearn` |
| Username | `geolearn` |
| Password | `geolearn` |

The host is `postgis` (the Docker service name), not `localhost`, because pgAdmin resolves it over the internal Docker network.

## Notes

- pgAdmin state persists in the `pgadmin_data` named volume across restarts.
- Service starts only after `postgis` passes its healthcheck.
