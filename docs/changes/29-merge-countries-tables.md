# 29 — Merge countries tables

## Summary

Merged `focus_countries` into the EF-managed `countries` table by adding `lat`, `lng`, and `zoom` columns. The old `focus_countries` table is dropped.

## Files changed

| File | Change |
|------|--------|
| `src/GeoLearn.Api/Migrations/20260304130000_MergeCountriesTables.cs` | New migration: adds columns (nullable), upserts 20 country rows, enforces NOT NULL, drops `focus_countries` |
| `src/GeoLearn.Api/Models/Country.cs` | Added `Lat`, `Lng`, `Zoom` properties |
| `src/GeoLearn.Api/Data/AppDbContext.cs` | Added column mappings for `lat`, `lng`, `zoom` in the `Country` entity block |
| `src/GeoLearn.Api/Migrations/AppDbContextModelSnapshot.cs` | Added `Lat`, `Lng`, `Zoom` property entries to the `Country` entity snapshot |
| `src/GeoLearn.Api/Controllers/WorkObjectsController.cs` | `GetCountries` projection now includes `Lat`, `Lng`, `Zoom` |

## Migration strategy

1. Columns added as nullable so existing rows are not rejected.
2. All 20 countries upserted via `ON CONFLICT (code) DO UPDATE` — existing LK/SE rows get coordinates, new rows are inserted.
3. Columns then altered to `NOT NULL`.
4. `focus_countries` dropped via `DROP TABLE IF EXISTS`.

Down migration recreates `focus_countries` from the `countries` data, then removes the three columns.
