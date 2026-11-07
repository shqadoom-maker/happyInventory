using Bariza.Application.Interfaces.Repositories;
using Bariza.Core.Entities;
using Bariza.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bariza.Infrastructure.Repositories;

public class CityRepository : ICityRepository
{
    private readonly AppDbContext _db;

    public CityRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<City>> GetAllAsync(
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

        return await query
            .OrderBy(city => city.Country != null ? city.Country.Name : string.Empty)
            .ThenBy(city => city.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<City?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.Cities
            .Include(city => city.Country)
            .FirstOrDefaultAsync(city => city.Id == id, cancellationToken);
    }

    public Task<bool> CountryExistsAsync(int countryId, CancellationToken cancellationToken = default)
    {
        return _db.Countries.AnyAsync(country => country.Id == countryId, cancellationToken);
    }

    public Task<bool> AnyDuplicateAsync(
        string name,
        int countryId,
        int? excludedId,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.ToLowerInvariant();

        return _db.Cities.AnyAsync(
            city =>
                (excludedId == null || city.Id != excludedId) &&
                city.CountryId == countryId &&
                city.Name.ToLower() == normalized,
            cancellationToken);
    }

    public Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.Warehouses.AnyAsync(
            warehouse => warehouse.CityId == id,
            cancellationToken);
    }

    public async Task AddAsync(City city, CancellationToken cancellationToken = default)
    {
        await _db.Cities.AddAsync(city, cancellationToken);
    }

    public void Remove(City city)
    {
        _db.Cities.Remove(city);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
