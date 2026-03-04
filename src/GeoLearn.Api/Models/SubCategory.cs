namespace GeoLearn.Api.Models;

public class SubCategory
{
    public int Id { get; set; }
    public int MainCategoryId { get; set; }

    /// <summary>
    /// Display label and filter value — must match the actual value stored in
    /// the work_objects column indicated by MainCategory.FieldName.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    public MainCategory MainCategory { get; set; } = null!;
}
