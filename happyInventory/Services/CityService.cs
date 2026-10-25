using happyInventory.Data;
using happyInventory.DTOs.Cities;
using happyInventory.Models;
using happyInventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace happyInventory.Services;

public class CityService : ICityService
{
    private readonly AppDbContext _db;

    public CityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CityDto>> GetAllAsync(
        int? countryId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Cities
            .AsNoTracking()
            .Include(city => city.Country)
            .AsQueryable();

        if (countryId.HasValue)
        {
            query = query.Where(city => city.CountryId == countryId.Value);
        }

        var cities = await query
            .OrderBy(city => city.Country!.Name)
            .ThenBy(city => city.Name)
            .ToListAsync(cancellationToken);

        return cities.Select(ToDto).ToList();
    }

    public async Task<CityDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var city = await _db.Cities
            .AsNoTracking()
            .Include(candidate => candidate.Country)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (city is null)
        {
            throw new KeyNotFoundException("The requested city was not found.");
        }

        return ToDto(city);
    }

    public async Task<CityDto> CreateAsync(
        CityRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = Require(request.Name);
        await EnsureCountryExistsAsync(request.CountryId, cancellationToken);
        await EnsureUniqueAsync(name, request.CountryId, null, cancellationToken);

        var city = new City
        {
            Name = name,
            CountryId = request.CountryId,
        };

        _db.Cities.Add(city);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(city).Reference(candidate => candidate.Country).LoadAsync(cancellationToken);

        return ToDto(city);
    }

    public async Task<CityDto> UpdateAsync(
        int id,
        CityRequest request,
        CancellationToken cancellationToken = default)
    {
        var city = await _db.Cities
            .Include(candidate => candidate.Country)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (city is null)
        {
            throw new KeyNotFoundException("The requested city was not found.");
        }

        var name = Require(request.Name);
        await EnsureCountryExistsAsync(request.CountryId, cancellationToken);
        await EnsureUniqueAsync(name, request.CountryId, id, cancellationToken);

        city.Name = name;
        city.CountryId = request.CountryId;

        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(city).Reference(candidate => candidate.Country).LoadAsync(cancellationToken);

        return ToDto(city);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var city = await _db.Cities.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (city is null)
        {
            throw new KeyNotFoundException("The requested city was not found.");
        }

        var inUse = await _db.Warehouses.AnyAsync(
            warehouse => warehouse.CityId == id,
            cancellationToken);

        if (inUse)
        {
            throw new InvalidOperationException(
                "The city cannot be deleted while it has warehouses.");
        }

        _db.Cities.Remove(city);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCountryExistsAsync(int countryId, CancellationToken cancellationToken)
    {
        var exists = await _db.Countries.AnyAsync(
            country => country.Id == countryId,
            cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("The selected country does not exist.");
        }
    }

    private async Task EnsureUniqueAsync(
        string name,
        int countryId,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();

        var duplicate = await _db.Cities.AnyAsync(
            city =>
                (excludedId == null || city.Id != excludedId) &&
                city.CountryId == countryId &&
                city.Name.ToLower() == normalized,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A city with the same name already exists in that country.");
        }
    }

    private static string Require(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("City name is required.");
        }

        return trimmed;
    }

    private static CityDto ToDto(City city) => new()
    {
        Id = city.Id,
        Name = city.Name,
        CountryId = city.CountryId,
        CountryName = city.Country?.Name,
    };
}
