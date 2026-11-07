using Bariza.Application.Interfaces.Repositories;
using Bariza.Core.Entities;
using Bariza.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bariza.Infrastructure.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly AppDbContext _db;

    public WarehouseRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<Warehouse> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        // Note: Warehouse.Items is intentionally NOT included here to avoid
        // loading all item rows when only the count is needed. WarehouseService
        // handles null Items gracefully in ToDto.
        var query = _db.Warehouses
            .AsNoTracking()
            .Include(warehouse => warehouse.City)
            .Include(warehouse => warehouse.Country)
            .AsQueryable();

        var term = search?.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(warehouse =>
                EF.Functions.Like(warehouse.Name, $"%{term}%") ||
                EF.Functions.Like(warehouse.Address, $"%{term}%") ||
                (warehouse.City != null && EF.Functions.Like(warehouse.City.Name, $"%{term}%")) ||
                (warehouse.Country != null && EF.Functions.Like(warehouse.Country.Name, $"%{term}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(warehouse => warehouse.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        // Note: Warehouse.Items is intentionally NOT included here to avoid
        // loading all item rows. Use GetByIdAsync if item details are needed.
        return await _db.Warehouses
            .AsNoTracking()
            .Include(warehouse => warehouse.City)
            .Include(warehouse => warehouse.Country)
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Warehouse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.Warehouses
            .Include(warehouse => warehouse.City)
            .Include(warehouse => warehouse.Country)
            .Include(warehouse => warehouse.Items)
            .FirstOrDefaultAsync(warehouse => warehouse.Id == id, cancellationToken);
    }

    public Task<Warehouse?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.Warehouses
            .Include(warehouse => warehouse.Items)
            .FirstOrDefaultAsync(warehouse => warehouse.Id == id, cancellationToken);
    }

    public Task<City?> GetCityWithCountryAsync(
        int cityId,
        CancellationToken cancellationToken = default)
    {
        return _db.Cities
            .Include(city => city.Country)
            .FirstOrDefaultAsync(city => city.Id == cityId, cancellationToken);
    }

    public Task<bool> AnyDuplicateNameAsync(
        string name,
        int? excludedId,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.ToLowerInvariant();

        return _db.Warehouses.AnyAsync(
            warehouse =>
                (excludedId == null || warehouse.Id != excludedId) &&
                warehouse.Name.ToLower() == normalized,
            cancellationToken);
    }

    public async Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        await _db.Warehouses.AddAsync(warehouse, cancellationToken);
    }

    public void Remove(Warehouse warehouse)
    {
        _db.Warehouses.Remove(warehouse);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
