using Dapper;
using GeoLearn.Api.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using Npgsql;
using System.IO.Compression;

namespace GeoLearn.Api.Services;

/// <summary>
/// Ingests a shapefile .zip and persists features into PostGIS as work_objects.
///
/// Pipeline:
///   1. Extract .zip to a temp directory
///   2. Detect source SRID from .prj (defaults to 3006 if absent/unrecognised)
///   3. Stream features with NTS ShapefileDataReader (one at a time — no full-load)
///   4. Repair invalid geometries with Buffer(0)
///   5. Accumulate into chunks of <see cref="ChunkSize"/>
///   6. INSERT each chunk via Dapper in a single transaction.
///      Geometry transform and area computation happen inline on INSERT:
///        geom   = ST_Transform(ST_SetSRID(ST_GeomFromWKB(@GeomWkb), sourceSrid), 5234)
///        area_ha = COALESCE(@AreaHa, ST_Area(transformed_geom) / 10000.0)
///      This works for any source CRS and avoids a post-insert table scan.
/// </summary>
public class ShapefileImportService(AppDbContext db, ILogger<ShapefileImportService> logger)
{
    private const int ChunkSize = 500;

    // Attribute column → WorkObject property, case-insensitive.
    // Covers Swedish forestry variants + common OSM field names.
    private static readonly Dictionary<string, string> AttributeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Area
        ["AREAL"]        = "AreaHa",
        ["area_ha"]      = "AreaHa",
        ["AREAL_HA"]     = "AreaHa",
        ["area_hectare"] = "AreaHa",

        // Compartment / division
        ["AVDNR"]        = "CompartmentId",
        ["Avdelning"]    = "CompartmentId",
        ["avd_nr"]       = "CompartmentId",

        // Species / type
        ["TRAD"]         = "SpeciesCode",
        ["tradslag"]     = "SpeciesCode",
        ["TradSlag"]     = "SpeciesCode",
        ["building"]     = "SpeciesCode",   // OSM: building type
        ["amenity"]      = "SpeciesCode",   // OSM: amenity type

        // Age
        ["ALDER"]        = "AgeYears",
        ["Alder_ar"]     = "AgeYears",
        ["alder_ar"]     = "AgeYears",

        // Name
        ["NAMN"]         = "Name",
        ["namn"]         = "Name",
        ["Name"]         = "Name",
        ["name"]         = "Name",
    };

    // Intermediate record populated from shapefile attributes before INSERT.
    private sealed class FeatureRecord
    {
        public string Name { get; set; } = "unnamed";
        public double? AreaHa { get; set; }
        public string? CompartmentId { get; set; }
        public string? SpeciesCode { get; set; }
        public int? AgeYears { get; set; }
        public int SourceSrid { get; set; }
        public byte[] GeomWkb { get; set; } = [];   // WKB bytes for ST_GeomFromWKB
    }

    public async Task<int> ImportAsync(Stream zipStream, string countryCode = "LK", CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"forestlink_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // 1. Extract zip
            logger.LogInformation("Extracting upload to {Dir}", tempDir);
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                archive.ExtractToDirectory(tempDir);

            // 2. Locate .shp
            var shpFile = Directory.GetFiles(tempDir, "*.shp", SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidOperationException("No .shp file found in the uploaded zip.");

            // 3. Detect CRS
            var prjFile = Path.ChangeExtension(shpFile, ".prj");
            var sourceSrid = DetectSrid(prjFile);
            logger.LogInformation("Source SRID: {Srid}", sourceSrid);

            // 4–6. Stream → chunk → INSERT with ST_Transform
            var total = await ReadAndInsertAsync(shpFile, sourceSrid, countryCode, ct);

            return total;
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private async Task<int> ReadAndInsertAsync(string shpPath, int sourceSrid, string countryCode, CancellationToken ct)
    {
        var factory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(sourceSrid);
        using var reader = new ShapefileDataReader(shpPath, factory);

        var header = reader.DbaseHeader;
        var columnNames = Enumerable.Range(0, header.NumFields)
            .Select(i => header.Fields[i].Name)
            .ToArray();
        logger.LogInformation("Shapefile columns: {Cols}", string.Join(", ", columnNames));

        // Get a raw Npgsql connection from EF Core for Dapper.
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        var wkbWriter = new WKBWriter();
        var chunk = new List<FeatureRecord>(ChunkSize);
        var total = 0;

        while (reader.Read())
        {
            ct.ThrowIfCancellationRequested();

            var rawGeom = reader.Geometry;
            if (rawGeom == null) continue;

            // Repair self-intersections before serialising to WKB.
            var geom = rawGeom.IsValid ? rawGeom : rawGeom.Buffer(0);

            var rec = new FeatureRecord
            {
                SourceSrid = sourceSrid,
                GeomWkb    = wkbWriter.Write(geom),
            };

            // Map known attribute columns
            for (int i = 0; i < columnNames.Length; i++)
            {
                var value = reader.GetValue(i + 1); // index 0 is the record number
                if (value == null || value is DBNull) continue;
                if (AttributeMap.TryGetValue(columnNames[i], out var prop))
                    ApplyAttribute(rec, prop, value);
            }

            chunk.Add(rec);

            if (chunk.Count >= ChunkSize)
            {
                await FlushChunkAsync(conn, chunk, sourceSrid, countryCode, ct);
                total += chunk.Count;
                chunk.Clear();
                logger.LogInformation("Inserted {Total} rows so far…", total);
            }
        }

        if (chunk.Count > 0)
        {
            await FlushChunkAsync(conn, chunk, sourceSrid, countryCode, ct);
            total += chunk.Count;
        }

        logger.LogInformation("Shapefile import complete. Total rows: {Total}", total);
        return total;
    }

    /// <summary>
    /// Batch-inserts a chunk of features in a single transaction.
    /// PostGIS transforms the geometry from the source CRS to EPSG:5234 on INSERT.
    /// </summary>
    private static async Task FlushChunkAsync(
        NpgsqlConnection conn,
        IEnumerable<FeatureRecord> chunk,
        int sourceSrid,
        string countryCode,
        CancellationToken ct)
    {
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            await conn.ExecuteAsync(
                $"""
                INSERT INTO work_objects
                    (name, area_ha, compartment_id, species_code, age_years, source_srid, country_code, geom)
                SELECT
                    @Name,
                    COALESCE(@AreaHa::double precision,
                             ST_Area(ST_Transform(ST_SetSRID(ST_GeomFromWKB(@GeomWkb), {sourceSrid}), 5234)) / 10000.0),
                    @CompartmentId, @SpeciesCode, @AgeYears, @SourceSrid,
                    '{countryCode}',
                    ST_Transform(ST_SetSRID(ST_GeomFromWKB(@GeomWkb), {sourceSrid}), 5234)
                """,
                chunk,
                transaction: tx);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static void ApplyAttribute(FeatureRecord rec, string prop, object value)
    {
        var str = value.ToString();
        switch (prop)
        {
            case "Name":
                if (!string.IsNullOrWhiteSpace(str)) rec.Name = str;
                break;
            case "AreaHa":
                if (double.TryParse(str, out var area)) rec.AreaHa = area;
                break;
            case "CompartmentId":
                rec.CompartmentId = str;
                break;
            case "SpeciesCode":
                if (!string.IsNullOrWhiteSpace(str)) rec.SpeciesCode = str;
                break;
            case "AgeYears":
                if (int.TryParse(str, out var age)) rec.AgeYears = age;
                break;
        }
    }

    /// <summary>
    /// Reads the .prj WKT and returns an EPSG code.
    /// Handles both WKT1 (ESRI/OGC) and WKT2 formats produced by modern geopandas/pyproj.
    /// </summary>
    private static int DetectSrid(string prjPath)
    {
        if (!File.Exists(prjPath)) return 3006;
        var wkt = File.ReadAllText(prjPath);

        if (wkt.Contains("SWEREF99_TM", StringComparison.OrdinalIgnoreCase) ||
            wkt.Contains("SWEREF99 TM", StringComparison.OrdinalIgnoreCase))
            return 3006;

        if (wkt.Contains("RT90", StringComparison.OrdinalIgnoreCase))
            return 3021;

        // WGS 84 / EPSG:4326 — matches WKT1 (GCS_WGS_1984, WGS_1984, WGS 1984),
        // WKT2 ("WGS 84"), and the explicit EPSG authority tag ID["EPSG",4326].
        if (wkt.Contains("GCS_WGS_1984", StringComparison.OrdinalIgnoreCase) ||
            wkt.Contains("WGS 1984",     StringComparison.OrdinalIgnoreCase) ||
            wkt.Contains("WGS_1984",     StringComparison.OrdinalIgnoreCase) ||
            wkt.Contains("\"WGS 84\"",   StringComparison.OrdinalIgnoreCase) ||
            wkt.Contains("EPSG\",4326",  StringComparison.OrdinalIgnoreCase))
            return 4326;

        if (wkt.Contains("SWEREF99", StringComparison.OrdinalIgnoreCase))
            return 3006;

        return 3006;
    }
}
