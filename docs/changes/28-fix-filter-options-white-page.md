# 28 — Fix: White page when selecting country

## Root cause

`GET /workobjects/filter-options?country=LK` was returning an array
`[{ id, label, fieldName }, ...]` but the frontend expected
`{ mainCategoryLabel, subCategoryLabel, mainCategoryValues }`.

`setMainValues(data.mainCategoryValues)` set `mainValues` to `undefined`.
The render then called `undefined.map(...)` → uncaught TypeError → React
unmounted the tree → white page.

A secondary bug: `GET /workobjects` accepted `int? mainCategoryId` /
`int? subCategoryId` but the frontend sends `mainCategory` / `subCategory`
as string values, so filters were silently ignored.

## What changed

### `src/GeoLearn.Api/Controllers/WorkObjectsController.cs`

**`GetFilterOptions`** — completely rewritten:
- Parameter changed from `int? mainCategoryId` → `string? mainCategory`
- First `main_categories` row (ordered by id) = main dropdown; second = sub dropdown
- Without `mainCategory`: returns `{ mainCategoryLabel, subCategoryLabel, mainCategoryValues }`
  where `mainCategoryValues` = distinct DB values for that field
- With `mainCategory`: returns `{ subCategoryValues }` filtered by the selected main value
- Added `SafeCol()` whitelist to prevent SQL injection via dynamic column names

**`GetAll`** — parameter names fixed:
- `int? mainCategoryId` → `string? mainCategory`
- `int? subCategoryId`  → `string? subCategory`
- Field names resolved from `main_categories` table at query time
