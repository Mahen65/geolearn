namespace GeoLearn.Api.Models;

public class MainCategory
{
    public int Id { get; set; }
    public int CountryId { get; set; }

    /// <summary>Display label shown in the filter dropdown, e.g. "Forest Type".</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Column in work_objects used for filtering: "species_code" or "compartment_id".
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    public Country Country { get; set; } = null!;
    public ICollection<SubCategory> SubCategories { get; set; } = [];
}
