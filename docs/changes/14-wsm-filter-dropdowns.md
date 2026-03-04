# 14 — WSM Filter Dropdowns

## Problem
Loading all 3.7M NSDI features at once crashed the browser tab (Chrome "Aw, Snap").

## Solution
Replaced the plain Load/Unload buttons with a filter panel. The API now requires at least one filter and caps results at 500 rows.

## Files changed

### Backend
- `Controllers/WorkObjectsController.cs`
  - New `GET /workobjects/filter-options` — returns distinct values for `speciesCodes`, `compartmentIds`, `names`
  - Updated `GET /workobjects` — accepts `?speciesCode=`, `?compartmentId=`, `?name=` query params; returns 400 if none supplied; hard limit of 500 rows; response includes `count` and `limited` fields

### Frontend
- `src/api.js` — added `fetchFilterOptions()`, updated `fetchAllParcels(filters)` to pass query params
- `src/components/WsmFilterPanel.jsx` — new component: three dropdowns (Forest Type, Division, Name) + Load/Unload buttons + feature count display
- `src/App.jsx` — loads filter options on mount; wires `WsmFilterPanel`; tracks `wsmMeta` (count + limited); unloads map after upload/import

## Field mapping (NSDI → dropdown labels)
| Dropdown | DB column | NSDI source field |
|----------|-----------|-------------------|
| Forest Type | `species_code` | `description` |
| Division | `compartment_id` | `division` |
| Name | `name` | `forest_name` |

## Behaviour
- Dropdowns are disabled while data is loaded (must Unload first to change filters)
- "capped at 500" warning shown in red when result was truncated
- Upload / NSDI import success clears the loaded layer so user re-applies filters
