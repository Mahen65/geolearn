using Dapper;
using GeoLearn.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

namespace GeoLearn.Api.Services;

/// <summary>
/// Downloads forest boundary features from the Sri Lanka NSDI ArcGIS REST API
/// and inserts them into PostGIS as work_objects.
///
/// Pipeline:
///   1. Count total features (optional log)
///   2. Page through the REST API in batches of 1,000 (maxRecordCount limit)
///   3. For each page, batch-INSERT within a transaction using Dapper
///   4. Geometry arrives as WGS 84 GeoJSON (EPSG:4326); PostGIS transforms
///      it to Sri Lanka Grid 1999 (EPSG:5234) on INSERT via ST_Transform
///   5. After all pages, fill in any NULL area_ha from ST_Area(geom)/10000
///
/// Source:
///   https://gisapps.nsdi.gov.lk/server/rest/services/SLNSDI/Boundary/MapServer/1
/// </summary>
public class NsdiImportService(AppDbContext db, HttpClient http, ILogger<NsdiImportService> logger)
{
    private const string QueryUrl =
        "https://gisapps.nsdi.gov.lk/server/rest/services/SLNSDI/Boundary/MapServer/1/query";

    private const int PageSize = 1000;

    // Only the fields we actually use — keeps the response payload smaller.
    private const string OutFields = "forest_name,area_final,division,description,district,gfcode";

    public async Task<int> ImportAsync(CancellationToken ct = default)
    {
        // Wipe existing data so a re-import doesn't create duplicates.
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE work_objects RESTART IDENTITY", ct);
        logger.LogInformation("Cleared work_objects; starting NSDI import");

        // Obtain a raw Npgsql connection from EF Core — needed for Dapper.
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        var total = 0;
        var offset = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var url = $"{QueryUrl}?where=1%3D1" +
                      $"&outFields={Uri.EscapeDataString(OutFields)}" +
                      $"&resultOffset={offset}" +
                      $"&resultRecordCount={PageSize}" +
                      "&f=geojson";

            logger.LogInformation("Fetching page offset={Offset}", offset);

            string json;
            try
            {
                json = await http.GetStringAsync(url, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "HTTP request failed at offset {Offset}", offset);
                throw;
            }

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("features", out var featuresEl))
                break;

            var features = featuresEl.EnumerateArray().ToList();
            if (features.Count == 0) break;

            // Batch-insert this page within a single transaction.
            await using var tx = await conn.BeginTransactionAsync(ct);
            try
            {
                foreach (var feat in features)
                {
                    var props = feat.GetProperty("properties");
                    var geomJson = feat.GetProperty("geometry").GetRawText();

                    var forestName = GetString(props, "forest_name");
                    var description = GetString(props, "description"); // e.g. "Dense Forests"
                    var district = GetString(props, "district");

                    // Build a meaningful name: prefer forest_name; fall back to
                    // "<description> — <district>" for unnamed features.
                    var name = !string.IsNullOrWhiteSpace(forestName)
                        ? forestName
                        : $"{description ?? "Forest"} — {district ?? "unknown"}";

                    await conn.ExecuteAsync(
                        """
                        INSERT INTO work_objects
                            (name, area_ha, compartment_id, species_code, source_srid, country_code, geom)
                        SELECT
                            @name,
                            @areaHa,
                            @compartmentId,
                            @speciesCode,
                            4326,
                            'LK',
                            -- ST_GeomFromGeoJSON parses the WGS 84 GeoJSON geometry string.
                            -- ST_SetSRID tags it with EPSG:4326 so PostGIS knows the source CRS.
                            -- ST_Transform reprojects to EPSG:5234 (Sri Lanka Grid 1999) for storage.
                            ST_Transform(ST_SetSRID(ST_GeomFromGeoJSON(@geomJson), 4326), 5234)
                        """,
                        new
                        {
                            name,
                            areaHa      = GetDouble(props, "area_final"),   // hectares from NSDI
                            compartmentId = GetString(props, "division"),
                            speciesCode = description,                       // forest type description
                            geomJson,
                        },
                        transaction: tx);
                }

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }

            total += features.Count;
            logger.LogInformation("Page done: {Count} features | running total: {Total}", features.Count, total);

            offset += PageSize;

            // If the page returned fewer records than PageSize, this was the last page.
            if (features.Count < PageSize) break;
        }

        // Fill in area_ha for any rows where area_final was NULL in the source.
        // ST_Area on EPSG:5234 geometry returns m²; divide by 10,000 for hectares.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE work_objects SET area_ha = ST_Area(geom) / 10000.0 WHERE area_ha IS NULL", ct);

        logger.LogInformation("NSDI import complete. Total inserted: {Total}", total);
        return total;
    }

    private static string? GetString(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();
        return null;
    }

    private static double? GetDouble(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number)
            return v.GetDouble();
        return null;
    }
}
