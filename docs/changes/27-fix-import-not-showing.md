# 27 — Fix: Imported shapefile not showing on map

## Root cause

The shapefile-tool's `init_db()` (added in change 26) created a `countries` table with schema
`(id, name, lat, lng, zoom)` before the .NET API had a chance to apply its `AddMultiCategorySystem`
migration. That migration also creates a `countries` table (with `id, code, name`) plus
`main_categories` and `sub_categories`. Because the table already existed with a conflicting schema,
the migration was blocked from ever running.

Consequences:
- `main_categories` and `sub_categories` tables were never created
- `countries.code` column didn't exist → `GET /workobjects/countries` threw a DB error → the country
  dropdown in WsmFilterPanel was always empty → user could not click "Load" to see imported data

## What was done

### 1. Rename shapefile-tool table (`src/shapefile-tool/app.py`)
All references to the `countries` table in the shapefile tool were changed to `focus_countries` so it
no longer conflicts with the .NET API's `countries` table.
Route `/countries` kept the same path (only the table name changed internally).

### 2. DB fix
```sql
ALTER TABLE countries RENAME TO focus_countries;
```

### 3. Rebuild API + apply migration
Rebuilt the `api` Docker image (it had never been rebuilt since `AddMultiCategorySystem` was written,
so the migration class wasn't compiled in). On startup, `AddMultiCategorySystem` applied successfully:
- Dropped `country_configs`
- Created `countries (id, code, name)` + unique index on code
- Created `main_categories (id, country_id, label, field_name)`
- Created `sub_categories (id, main_category_id, label)`

### 4. Seed reference data
```sql
INSERT INTO countries (code, name) VALUES ('LK', 'Sri Lanka'), ('SE', 'Sweden');
INSERT INTO main_categories (country_id, label, field_name)
  SELECT id, 'Forest Type', 'species_code' FROM countries WHERE code = 'LK';
INSERT INTO main_categories (country_id, label, field_name)
  SELECT id, 'Division', 'compartment_id' FROM countries WHERE code = 'LK';
```

### 5. Rebuild shapefile-tool
Deployed updated `app.py` using `focus_countries`.

## Files changed
- `src/shapefile-tool/app.py` — table name `countries` → `focus_countries`
