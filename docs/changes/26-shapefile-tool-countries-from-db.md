# 26 — Shapefile Tool: Countries from Database

## What changed
- `src/shapefile-tool/requirements.txt` — added `psycopg2-binary>=2.9`
- `src/shapefile-tool/app.py` — DB init, seed, and `GET /countries` endpoint
- `src/shapefile-tool/templates/index.html` — dropdown populated via fetch instead of hardcoded options
- `docker-compose.yml` — `shapefile-tool` now gets `DATABASE_URL` env var and `depends_on: postgis`

## Database table

```sql
CREATE TABLE IF NOT EXISTS countries (
    id   SERIAL PRIMARY KEY,
    name TEXT             NOT NULL,
    lat  DOUBLE PRECISION NOT NULL,
    lng  DOUBLE PRECISION NOT NULL,
    zoom INTEGER          NOT NULL
);
```

Seeded with 20 countries on first startup if the table is empty.

## API

`GET /countries` — returns JSON array of `{name, lat, lng, zoom}` ordered by name.

## Frontend

On page load, `loadCountries()` fetches `/countries` and builds the `<option>` list dynamically.
Shows "— loading… —" while fetching and "— could not load countries —" on error.

## Adding/editing countries

Connect to pgAdmin (`http://localhost:5050`) and INSERT/UPDATE rows in the `countries` table directly.
