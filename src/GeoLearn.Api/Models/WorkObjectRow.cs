namespace GeoLearn.Api.Models;

/// <summary>
/// Keyless entity type used for raw PostGIS SQL result mapping.
/// Column names are configured in AppDbContext.OnModelCreating.
/// Never maps to a database table — no migration is generated for it.
/// </summary>
public class WorkObjectRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? AreaHa { get; set; }
    public string? CompartmentId { get; set; }
    public string? SpeciesCode { get; set; }
    public int? AgeYears { get; set; }
    public int SourceSrid { get; set; }

    /// <summary>GeoJSON string produced by ST_AsGeoJSON(ST_Transform(geom, 4326)).</summary>
    public string GeoJson { get; set; } = string.Empty;

    /// <summary>
    /// Area in hectares computed live by ST_Area(geom)/10000.
    /// Only populated by the GetById query; null for collection queries.
    /// </summary>
    public double? AreaHaLive { get; set; }
}
