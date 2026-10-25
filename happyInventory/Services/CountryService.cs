using happyInventory.Data;
using happyInventory.DTOs.Countries;
using happyInventory.Models;
using happyInventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace happyInventory.Services;

public class CountryService : ICountryService
{
    private readonly AppDbContext _db;

    public CountryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CountryDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var countries = await _db.Countries
            .AsNoTracking()
            .OrderBy(country => country.Name)
            .ToListAsync(cancellationToken);

        return countries.Select(ToDto).ToList();
    }

    public async Task<CountryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var country = await _db.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (country is null)
        {
            throw new KeyNotFoundException("The requested country was not found.");
        }

        return ToDto(country);
    }

    public async Task<CountryDto> CreateAsync(
        CountryRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = Require(request.Name, "Country name");
        var code = Require(request.Code, "Country code").ToUpperInvariant();

        await EnsureUniqueAsync(name, code, null, cancellationToken);

        var country = new Country
        {
            Name = name,
            Code = code,
        };

        _db.Countries.Add(country);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(country);
    }

    public async Task<CountryDto> UpdateAsync(
        int id,
        CountryRequest request,
        CancellationToken cancellationToken = default)
    {
        var country = await _db.Countries.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (country is null)
        {
            throw new KeyNotFoundException("The requested country was not found.");
        }

        var name = Require(request.Name, "Country name");
        var code = Require(request.Code, "Country code").ToUpperInvariant();

        await EnsureUniqueAsync(name, code, id, cancellationToken);

        country.Name = name;
        country.Code = code;

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(country);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var country = await _db.Countries.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (country is null)
        {
            throw new KeyNotFoundException("The requested country was not found.");
        }

        var inUse = await _db.Cities.AnyAsync(city => city.CountryId == id, cancellationToken)
            || await _db.Warehouses.AnyAsync(warehouse => warehouse.CountryId == id, cancellationToken);

        if (inUse)
        {
            throw new InvalidOperationException(
                "The country cannot be deleted while it has cities or warehouses.");
        }

        _db.Countries.Remove(country);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueAsync(
        string name,
        string code,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.ToLowerInvariant();
        var normalizedCode = code.ToLowerInvariant();

        var duplicate = await _db.Countries.AnyAsync(
            country =>
                (excludedId == null || country.Id != excludedId) &&
                (country.Name.ToLower() == normalizedName || country.Code.ToLower() == normalizedCode),
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A country with the same name or code already exists.");
        }
    }

    private static string Require(string? value, string field)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException($"{field} is required.");
        }

        return trimmed;
    }

    private static CountryDto ToDto(Country country) => new()
    {
        Id = country.Id,
        Name = country.Name,
        Code = country.Code,
    };
}
