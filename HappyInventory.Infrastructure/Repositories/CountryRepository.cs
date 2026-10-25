using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Core.Entities;
using HappyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyInventory.Infrastructure.Repositories;

public class CountryRepository : ICountryRepository
{
    private readonly AppDbContext _db;

    public CountryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Countries
            .AsNoTracking()
            .OrderBy(country => country.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Country?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.Countries.FirstOrDefaultAsync(country => country.Id == id, cancellationToken);
    }

    public Task<bool> AnyDuplicateAsync(
        string name,
        string code,
        int? excludedId,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToLowerInvariant();
        var normalizedCode = code.ToLowerInvariant();

        return _db.Countries.AnyAsync(
            country =>
                (excludedId == null || country.Id != excludedId) &&
                (country.Name.ToLower() == normalizedName ||
                 country.Code.ToLower() == normalizedCode),
            cancellationToken);
    }

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        var hasCities = await _db.Cities.AnyAsync(
            city => city.CountryId == id,
            cancellationToken);

        if (hasCities)
        {
            return true;
        }

        return await _db.Warehouses.AnyAsync(
            warehouse => warehouse.CountryId == id,
            cancellationToken);
    }

    public async Task AddAsync(Country country, CancellationToken cancellationToken = default)
    {
        await _db.Countries.AddAsync(country, cancellationToken);
    }

    public void Remove(Country country)
    {
        _db.Countries.Remove(country);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
