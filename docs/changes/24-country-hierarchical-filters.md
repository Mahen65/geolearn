# Change 24: Country Hierarchical Filter System

## Summary
Replaced the flat Forest Type / Division / Name filter panel with a 3-tier Country → Main Category → Sub Category hierarchy. Filter labels and configured countries are stored in a new `country_configs` database table, making the system multi-country and fully configurable without code changes. Sub-category cascades from the main category selection.

## Database
- **New table `country_configs`**: `country_code` (PK), `country_name`, `main_category_label`, `sub_category_label`
- **Seeded**: Sri Lanka (`LK`) with labels "Forest Type" / "Division"
- **`work_objects`**: Added `country_code VARCHAR(10)` column; existing rows back-filled to `'LK'`
- **Migration**: `20260303061128_AddCountryFilters`

## Backend
- `Models/CountryConfig.cs` — new EF Core entity
- `Models/WorkObject.cs` — added `CountryCode` property
- `Data/AppDbContext.cs` — registered `CountryConfigs` DbSet and column mapping
- `Controllers/WorkObjectsController.cs`:
  - New `GET /workobjects/countries` — returns all configured countries with labels
  - Replaced `GET /workobjects/filter-options` — now takes `country` (required) and optional `mainCategory`; without mainCategory returns main category values + labels; with mainCategory returns cascaded sub category values
  - `GET /workobjects` — filter params changed from `speciesCode/compartmentId/name` to `country/mainCategory/subCategory`
- `Services/NsdiImportService.cs` — inserts `country_code = 'LK'` on every row

## Frontend
- `api.js` — new `fetchCountries()`, updated `fetchFilterOptions(country, mainCategory?)`, updated `streamParcels` params
- `components/WsmFilterPanel.jsx` — complete rewrite: Country dropdown loads on mount; selecting country loads main category options; selecting main category cascades sub category load; all labels are dynamic from server
- `App.jsx` — removed `filterOptions` state and `fetchFilterOptions` useEffect; `WsmFilterPanel` now owns its own data fetching

## Adding More Countries
Insert a row into `country_configs` and ensure the corresponding `work_objects` rows have the matching `country_code`. No code changes required.
