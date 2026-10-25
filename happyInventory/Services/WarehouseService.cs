using happyInventory.Common;
using happyInventory.Data;
using happyInventory.DTOs.Warehouses;
using happyInventory.Models;
using happyInventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace happyInventory.Services;

public class WarehouseService : IWarehouseService
{
    private const int MaxPageSize = 10;
    private readonly AppDbContext _db;

    public WarehouseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<WarehouseDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

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
                warehouse.City!.Name.ToLower().Contains(term) ||
                warehouse.Country!.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        var warehouses = await query
            .OrderBy(warehouse => warehouse.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseDto>
        {
            Items = warehouses.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
        };
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Include(warehouse => warehouse.City)
            .Include(warehouse => warehouse.Country)
            .Include(warehouse => warehouse.Items)
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync(cancellationToken);

        return warehouses.Select(ToDto).ToList();
    }

    public async Task<WarehouseDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _db.Warehouses
            .AsNoTracking()
            .Include(candidate => candidate.City)
            .Include(candidate => candidate.Country)
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException("The requested warehouse was not found.");
        }

        return ToDto(warehouse);
    }

    public async Task<WarehouseDto> CreateAsync(
        WarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = Require(request.Name, "Warehouse name");
        var address = Require(request.Address, "Address");

        var city = await GetCityAsync(request.CityId, request.CountryId, cancellationToken);
        await EnsureUniqueNameAsync(name, null, cancellationToken);

        var warehouse = new Warehouse
        {
            Name = name,
            Address = address,
            CityId = city.Id,
            CountryId = city.CountryId,
        };

        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync(cancellationToken);

        warehouse.City = city;
        warehouse.Country = city.Country;

        return ToDto(warehouse);
    }

    public async Task<WarehouseDto> UpdateAsync(
        int id,
        WarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _db.Warehouses
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException("The requested warehouse was not found.");
        }

        var name = Require(request.Name, "Warehouse name");
        var address = Require(request.Address, "Address");

        var city = await GetCityAsync(request.CityId, request.CountryId, cancellationToken);
        await EnsureUniqueNameAsync(name, id, cancellationToken);

        warehouse.Name = name;
        warehouse.Address = address;
        warehouse.CityId = city.Id;
        warehouse.CountryId = city.CountryId;

        await _db.SaveChangesAsync(cancellationToken);

        warehouse.City = city;
        warehouse.Country = city.Country;

        return ToDto(warehouse);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException("The requested warehouse was not found.");
        }

        _db.Warehouses.Remove(warehouse);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<City> GetCityAsync(
        int cityId,
        int countryId,
        CancellationToken cancellationToken)
    {
        var city = await _db.Cities
            .Include(candidate => candidate.Country)
            .FirstOrDefaultAsync(candidate => candidate.Id == cityId, cancellationToken);

        if (city is null)
        {
            throw new InvalidOperationException("The selected city does not exist.");
        }

        if (city.CountryId != countryId)
        {
            throw new InvalidOperationException(
                "The selected city does not belong to the selected country.");
        }

        return city;
    }

    private async Task EnsureUniqueNameAsync(
        string name,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();

        var duplicate = await _db.Warehouses.AnyAsync(
            warehouse =>
                (excludedId == null || warehouse.Id != excludedId) &&
                warehouse.Name.ToLower() == normalized,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"A warehouse named '{name}' already exists.");
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

    private static WarehouseDto ToDto(Warehouse warehouse) => new()
    {
        Id = warehouse.Id,
        Name = warehouse.Name,
        Address = warehouse.Address,
        CityId = warehouse.CityId,
        CityName = warehouse.City?.Name ?? string.Empty,
        CountryId = warehouse.CountryId,
        CountryName = warehouse.Country?.Name ?? string.Empty,
        ItemCount = warehouse.Items.Count,
    };
}
