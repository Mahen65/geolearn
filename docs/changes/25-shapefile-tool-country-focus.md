# 25 — Shapefile Tool: Country Focus Dropdown

## What changed
- `src/shapefile-tool/templates/index.html`

## Summary
Added a **Focus country** dropdown to the shapefile tool panel. Selecting a country smoothly flies the map to that country's centre at an appropriate zoom level.

## Countries included
Sri Lanka (Colombo), Sweden, India, United States, United Kingdom, Australia, Germany, France, Japan, Brazil, China, Canada, South Africa, Nigeria, Indonesia, Bangladesh, Nepal, Lebanon, UAE, Rwanda.

## Implementation
- Dropdown `#countrySelect` placed above the export filename field.
- Each `<option>` value encodes `lat,lng,zoom`.
- `focusCountry(value)` parses the value and calls `map.flyTo()` with a 1.2 s animation.
- Custom CSS arrow replaces the native `<select>` appearance for visual consistency with the rest of the panel.
