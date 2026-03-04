using GeoLearn.Api.Data;
using GeoLearn.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoLearn.Api.Controllers;

[ApiController]
[Route("config")]
public class ConfigController(AppDbContext db) : ControllerBase
{
    // ─── Countries ────────────────────────────────────────────────────────────

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries(CancellationToken ct)
    {
        var list = await db.Countries
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Code, c.Name })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost("countries")]
    public async Task<IActionResult> CreateCountry([FromBody] CountryDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("code and name are required.");

        var country = new Country { Code = dto.Code.ToUpperInvariant(), Name = dto.Name.Trim() };
        db.Countries.Add(country);
        await db.SaveChangesAsync(ct);
        return Ok(new { country.Id, country.Code, country.Name });
    }

    [HttpPut("countries/{id:int}")]
    public async Task<IActionResult> UpdateCountry(int id, [FromBody] CountryDto dto, CancellationToken ct)
    {
        var country = await db.Countries.FindAsync([id], ct);
        if (country == null) return NotFound();

        country.Code = dto.Code.ToUpperInvariant();
        country.Name = dto.Name.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(new { country.Id, country.Code, country.Name });
    }

    [HttpDelete("countries/{id:int}")]
    public async Task<IActionResult> DeleteCountry(int id, CancellationToken ct)
    {
        var country = await db.Countries.FindAsync([id], ct);
        if (country == null) return NotFound();
        db.Countries.Remove(country);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ─── Main Categories ──────────────────────────────────────────────────────

    [HttpGet("countries/{countryId:int}/main-categories")]
    public async Task<IActionResult> GetMainCategories(int countryId, CancellationToken ct)
    {
        var list = await db.MainCategories
            .Where(m => m.CountryId == countryId)
            .OrderBy(m => m.Label)
            .Select(m => new { m.Id, m.Label, m.FieldName, m.CountryId })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost("countries/{countryId:int}/main-categories")]
    public async Task<IActionResult> CreateMainCategory(int countryId, [FromBody] MainCategoryDto dto, CancellationToken ct)
    {
        if (!await db.Countries.AnyAsync(c => c.Id == countryId, ct))
            return NotFound("Country not found.");

        if (!IsValidFieldName(dto.FieldName))
            return BadRequest("fieldName must be 'species_code' or 'compartment_id'.");

        var mc = new MainCategory { CountryId = countryId, Label = dto.Label.Trim(), FieldName = dto.FieldName };
        db.MainCategories.Add(mc);
        await db.SaveChangesAsync(ct);
        return Ok(new { mc.Id, mc.Label, mc.FieldName, mc.CountryId });
    }

    [HttpPut("main-categories/{id:int}")]
    public async Task<IActionResult> UpdateMainCategory(int id, [FromBody] MainCategoryDto dto, CancellationToken ct)
    {
        var mc = await db.MainCategories.FindAsync([id], ct);
        if (mc == null) return NotFound();

        if (!IsValidFieldName(dto.FieldName))
            return BadRequest("fieldName must be 'species_code' or 'compartment_id'.");

        mc.Label = dto.Label.Trim();
        mc.FieldName = dto.FieldName;
        await db.SaveChangesAsync(ct);
        return Ok(new { mc.Id, mc.Label, mc.FieldName, mc.CountryId });
    }

    [HttpDelete("main-categories/{id:int}")]
    public async Task<IActionResult> DeleteMainCategory(int id, CancellationToken ct)
    {
        var mc = await db.MainCategories.FindAsync([id], ct);
        if (mc == null) return NotFound();
        db.MainCategories.Remove(mc);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ─── Sub Categories ───────────────────────────────────────────────────────

    [HttpGet("main-categories/{mainCategoryId:int}/sub-categories")]
    public async Task<IActionResult> GetSubCategories(int mainCategoryId, CancellationToken ct)
    {
        var list = await db.SubCategories
            .Where(s => s.MainCategoryId == mainCategoryId)
            .OrderBy(s => s.Label)
            .Select(s => new { s.Id, s.Label, s.MainCategoryId })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost("main-categories/{mainCategoryId:int}/sub-categories")]
    public async Task<IActionResult> CreateSubCategory(int mainCategoryId, [FromBody] SubCategoryDto dto, CancellationToken ct)
    {
        if (!await db.MainCategories.AnyAsync(m => m.Id == mainCategoryId, ct))
            return NotFound("Main category not found.");

        var sc = new SubCategory { MainCategoryId = mainCategoryId, Label = dto.Label.Trim() };
        db.SubCategories.Add(sc);
        await db.SaveChangesAsync(ct);
        return Ok(new { sc.Id, sc.Label, sc.MainCategoryId });
    }

    /// <summary>
    /// Auto-populate sub categories from distinct values in work_objects.
    /// Only inserts values not already present — idempotent.
    /// </summary>
    [HttpPost("main-categories/{mainCategoryId:int}/sub-categories/sync")]
    public async Task<IActionResult> SyncSubCategories(int mainCategoryId, CancellationToken ct)
    {
        var mc = await db.MainCategories
            .Include(m => m.Country)
            .FirstOrDefaultAsync(m => m.Id == mainCategoryId, ct);

        if (mc == null) return NotFound("Main category not found.");

        // Fetch distinct values from work_objects for the right field + country.
        IQueryable<string?> valuesQuery = mc.FieldName switch
        {
            "species_code"   => db.WorkObjects.Where(w => w.CountryCode == mc.Country.Code).Select(w => w.SpeciesCode),
            "compartment_id" => db.WorkObjects.Where(w => w.CountryCode == mc.Country.Code).Select(w => w.CompartmentId),
            _                => Enumerable.Empty<string?>().AsQueryable()
        };

        var distinctValues = await valuesQuery
            .Where(v => v != null && v != "")
            .Distinct()
            .ToListAsync(ct);

        var existing = await db.SubCategories
            .Where(s => s.MainCategoryId == mainCategoryId)
            .Select(s => s.Label)
            .ToHashSetAsync(ct);

        var toAdd = distinctValues
            .Where(v => !existing.Contains(v!))
            .Select(v => new SubCategory { MainCategoryId = mainCategoryId, Label = v! })
            .ToList();

        if (toAdd.Count > 0)
        {
            db.SubCategories.AddRange(toAdd);
            await db.SaveChangesAsync(ct);
        }

        return Ok(new { synced = toAdd.Count, total = existing.Count + toAdd.Count });
    }

    [HttpPut("sub-categories/{id:int}")]
    public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] SubCategoryDto dto, CancellationToken ct)
    {
        var sc = await db.SubCategories.FindAsync([id], ct);
        if (sc == null) return NotFound();
        sc.Label = dto.Label.Trim();
        await db.SaveChangesAsync(ct);
        return Ok(new { sc.Id, sc.Label, sc.MainCategoryId });
    }

    [HttpDelete("sub-categories/{id:int}")]
    public async Task<IActionResult> DeleteSubCategory(int id, CancellationToken ct)
    {
        var sc = await db.SubCategories.FindAsync([id], ct);
        if (sc == null) return NotFound();
        db.SubCategories.Remove(sc);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsValidFieldName(string? name) =>
        name is "species_code" or "compartment_id";
}

// ─── Request DTOs ──────────────────────────────────────────────────────────────

public record CountryDto(string Code, string Name);
public record MainCategoryDto(string Label, string FieldName);
public record SubCategoryDto(string Label);
