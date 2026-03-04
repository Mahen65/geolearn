using GeoLearn.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoLearn.Api.Controllers;

[ApiController]
[Route("countries")]
public class CountriesController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Returns all countries in the reference table (regardless of whether
    /// they have any work objects). Used to populate country selectors.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var countries = await db.Countries
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Code, c.Name, c.Lat, c.Lng, c.Zoom })
            .ToListAsync(ct);

        return Ok(countries);
    }
}
