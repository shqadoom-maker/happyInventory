using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Core.Entities;
using HappyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyInventory.Infrastructure.Repositories;

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
        var query = _db.Warehouses
            .AsNoTracking()
            .Include(warehouse => warehouse.City)
            .Include(warehouse => warehouse.Country)
            .Include(warehouse => warehouse.Items)
            .AsQueryable();

        var term = search?.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(warehouse =>
                warehouse.Name.ToLower().Contains(term) ||
                warehouse.Address.ToLower().Contains(term) ||
                (warehouse.City != null && warehouse.City.Name.ToLower().Contains(term)) ||
                (warehouse.Country != null && warehouse.Country.Name.ToLower().Contains(term)));
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
        return await _db.Warehouses
            .AsNoTracking()
            .Include(warehouse => warehouse.City)
            .Include(warehouse => warehouse.Country)
            .Include(warehouse => warehouse.Items)
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
