using GeoLearn.Api.Data;
using GeoLearn.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GeoLearn.Api.Controllers;

[ApiController]
[Route("workobjects")]
public class WorkObjectsController(AppDbContext db, ShapefileImportService importer, NsdiImportService nsdiImporter) : ControllerBase
{
    /// <summary>
    /// Upload a shapefile .zip and import it into PostGIS.
    /// </summary>
    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string countryCode = "LK", CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return BadRequest("File must be a .zip containing a shapefile bundle.");

        await using var stream = file.OpenReadStream();
        var count = await importer.ImportAsync(stream, countryCode.ToUpperInvariant(), ct);

        return Ok(new { inserted = count, message = $"{count} work object(s) imported successfully." });
    }

    /// <summary>
    /// Fetches all Sri Lanka forest boundary features from the NSDI ArcGIS REST API
    /// and imports them into PostGIS. Replaces any previously imported data.
    /// </summary>
    [HttpPost("import-nsdi")]
    public async Task<IActionResult> ImportFromNsdi(CancellationToken ct)
    {
        var count = await nsdiImporter.ImportAsync(ct);
        return Ok(new { imported = count, message = $"{count} forest boundary features imported from NSDI Sri Lanka." });
    }

    /// <summary>
    /// Returns countries that have at least one work_object — used to populate
    /// the country dropdown in the filter panel (not the config page).
    /// </summary>
    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(CancellationToken ct)
    {
        var codes = await db.WorkObjects
            .Where(w => w.CountryCode != null)
            .Select(w => w.CountryCode!)
            .Distinct()
            .ToListAsync(ct);

        var countries = await db.Countries
            .Where(c => codes.Contains(c.Code))
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Code, c.Name, c.Lat, c.Lng, c.Zoom })
            .ToListAsync(ct);

        return Ok(countries);
    }

    /// <summary>
    /// Returns filter options for the map panel.
    ///
    /// The first main_category for the country drives the main dropdown;
    /// the second drives the sub dropdown.
    ///
    /// Without mainCategory: { mainCategoryLabel, subCategoryLabel, mainCategoryValues }
    /// With mainCategory:    { subCategoryValues } — distinct sub values where main field = mainCategory
    /// </summary>
    [HttpGet("filter-options")]
    public async Task<IActionResult> GetFilterOptions(
        [FromQuery] string country,
        [FromQuery] string? mainCategory,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(country))
            return BadRequest("country is required.");

        var countryEntity = await db.Countries.FirstOrDefaultAsync(c => c.Code == country, ct);
        if (countryEntity == null) return NotFound($"Country '{country}' not found.");

        // Ordered by id: index 0 = main filter, index 1 = sub filter.
        var cats = await db.MainCategories
            .Where(m => m.CountryId == countryEntity.Id)
            .OrderBy(m => m.Id)
            .ToListAsync(ct);

        var mainCat = cats.ElementAtOrDefault(0);
        var subCat  = cats.ElementAtOrDefault(1);

        if (string.IsNullOrEmpty(mainCategory))
        {
            // Return labels + distinct values for the main category field.
            var mainValues = new List<string>();
            if (mainCat != null && SafeCol(mainCat.FieldName) is string col)
            {
                mainValues = await db.Database
                    .SqlQueryRaw<string>(
                        $"SELECT DISTINCT {col} FROM work_objects WHERE country_code = @p0 AND {col} IS NOT NULL ORDER BY 1",
                        country)
                    .ToListAsync(ct);
            }

            return Ok(new
            {
                mainCategoryLabel  = mainCat?.Label ?? "Category",
                subCategoryLabel   = subCat?.Label  ?? "Sub Category",
                mainCategoryValues = mainValues,
            });
        }
        else
        {
            // Return distinct sub-category values filtered by the selected main value.
            var subValues = new List<string>();
            if (mainCat != null && subCat != null
                && SafeCol(mainCat.FieldName) is string mainCol
                && SafeCol(subCat.FieldName)  is string subCol)
            {
                subValues = await db.Database
                    .SqlQueryRaw<string>(
                        $"SELECT DISTINCT {subCol} FROM work_objects WHERE country_code = @p0 AND {mainCol} = @p1 AND {subCol} IS NOT NULL ORDER BY 1",
                        country, mainCategory)
                    .ToListAsync(ct);
            }

            return Ok(new { subCategoryValues = subValues });
        }
    }

    // Whitelist of columns allowed in dynamic SQL to prevent injection.
    private static string? SafeCol(string? fieldName) => fieldName switch
    {
        "species_code"   => "species_code",
        "compartment_id" => "compartment_id",
        _                => null,
    };

    /// <summary>
    /// Streams filtered work objects as NDJSON.
    /// Filters: country, mainCategory (value string), subCategory (value string).
    /// Field names are resolved from main_categories for the given country.
    /// </summary>
    [HttpGet]
    public async Task GetAll(
        [FromQuery] string? country,
        [FromQuery] string? mainCategory,
        [FromQuery] string? subCategory,
        CancellationToken ct)
    {
        var conditions = new List<string>();
        var parameters = new List<object>();

        if (!string.IsNullOrEmpty(country))
        {
            conditions.Add($"country_code = {{{parameters.Count}}}");
            parameters.Add(country);
        }

        // Resolve field names from main_categories and apply value filters.
        if ((!string.IsNullOrEmpty(mainCategory) || !string.IsNullOrEmpty(subCategory))
            && !string.IsNullOrEmpty(country))
        {
            var cats = await db.MainCategories
                .Where(m => m.Country.Code == country)
                .OrderBy(m => m.Id)
                .ToListAsync(ct);

            var mainCat = cats.ElementAtOrDefault(0);
            var subCat  = cats.ElementAtOrDefault(1);

            if (!string.IsNullOrEmpty(mainCategory) && mainCat != null && SafeCol(mainCat.FieldName) is string mc)
            {
                conditions.Add($"{mc} = {{{parameters.Count}}}");
                parameters.Add(mainCategory);
            }

            if (!string.IsNullOrEmpty(subCategory) && subCat != null && SafeCol(subCat.FieldName) is string sc)
            {
                conditions.Add($"{sc} = {{{parameters.Count}}}");
                parameters.Add(subCategory);
            }
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        var sql = $"""
            SELECT
                id,
                name,
                area_ha,
                compartment_id,
                species_code,
                age_years,
                source_srid,
                ST_AsGeoJSON(ST_Transform(geom, 4326)) AS geojson,
                NULL::double precision                 AS area_ha_live
            FROM work_objects
            {whereClause}
            ORDER BY id
            """;

        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["Cache-Control"]      = "no-cache";

        var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var count = 0;

        try
        {
            await foreach (var row in db.WorkObjectRows
                .FromSqlRaw(sql, parameters.ToArray())
                .AsAsyncEnumerable()
                .WithCancellation(ct))
            {
                var propsJson = JsonSerializer.Serialize(new
                {
                    id            = row.Id,
                    name          = row.Name,
                    areaHa        = row.AreaHa,
                    compartmentId = row.CompartmentId,
                    speciesCode   = row.SpeciesCode,
                    ageYears      = row.AgeYears,
                    sourceSrid    = row.SourceSrid,
                }, jsonOpts);

                var line = $"{{\"type\":\"Feature\",\"geometry\":{row.GeoJson},\"properties\":{propsJson}}}\n";
                await Response.WriteAsync(line, ct);

                if (++count % 200 == 0)
                    await Response.Body.FlushAsync(ct);
            }

            await Response.Body.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — expected, not an error.
        }
    }

    /// <summary>
    /// Returns a single work object as a GeoJSON Feature.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var row = await db.WorkObjectRows
            .FromSqlInterpolated($"""
                SELECT
                    id,
                    name,
                    area_ha,
                    compartment_id,
                    species_code,
                    age_years,
                    source_srid,
                    ST_AsGeoJSON(ST_Transform(geom, 4326)) AS geojson,
                    ST_Area(geom) / 10000.0                AS area_ha_live
                FROM work_objects
                WHERE id = {id}
                """)
            .SingleOrDefaultAsync(ct);

        if (row == null) return NotFound();

        return Ok(new
        {
            type = "Feature",
            geometry = JsonDocument.Parse(row.GeoJson).RootElement,
            properties = new
            {
                row.Id,
                row.Name,
                row.AreaHa,
                row.AreaHaLive,
                row.CompartmentId,
                row.SpeciesCode,
                row.AgeYears,
                row.SourceSrid,
            }
        });
    }
}
