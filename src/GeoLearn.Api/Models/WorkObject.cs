using NetTopologySuite.Geometries;

namespace GeoLearn.Api.Models;

/// <summary>
/// Represents a forest work object (parcel/compartment) stored in SWEREF99 TM (EPSG:3006).
/// </summary>
public class WorkObject
{
    public int Id { get; set; }

    /// <summary>Display name, e.g. "Avdelning 12".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Polygon geometry stored in SWEREF99 TM (EPSG:3006).
    /// ST_Transform to 4326 before serving as GeoJSON.
    /// </summary>
    public Geometry Geom { get; set; } = null!;

    // --- Normalised attributes (mapped from varied client shapefile column names) ---

    /// <summary>Area in hectares, computed from ST_Area(geom)/10000 in PostGIS.</summary>
    public double? AreaHa { get; set; }

    /// <summary>Compartment identifier (e.g. from AVDNR or Avdelning column).</summary>
    public string? CompartmentId { get; set; }

    /// <summary>Tree species code (e.g. from TRAD or tradslag column).</summary>
    public string? SpeciesCode { get; set; }

    /// <summary>Stand age in years (e.g. from ALDER or Alder_ar column).</summary>
    public int? AgeYears { get; set; }

    /// <summary>Source CRS EPSG code from the .prj file at import time.</summary>
    public int SourceSrid { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code, e.g. "LK". Links to country_configs.</summary>
    public string? CountryCode { get; set; }
}
