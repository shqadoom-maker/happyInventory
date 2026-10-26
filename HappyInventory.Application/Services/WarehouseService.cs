using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Warehouses;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Services;

public class WarehouseService : IWarehouseService
{
    private const int MaxPageSize = 10;
    private readonly IWarehouseRepository _warehouses;

    public WarehouseService(IWarehouseRepository warehouses)
    {
        _warehouses = warehouses;
    }

    public async Task<PagedResult<WarehouseDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (warehouses, totalCount) = await _warehouses.GetPagedAsync(
            page,
            pageSize,
            search,
            cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

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
        var warehouses = await _warehouses.GetAllAsync(cancellationToken);
        return warehouses.Select(ToDto).ToList();
    }

    public async Task<WarehouseDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouses.GetByIdAsync(id, cancellationToken);

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

        var city = await _warehouses.GetCityWithCountryAsync(request.CityId, cancellationToken);

        if (city is null)
        {
            throw new InvalidOperationException("The selected city does not exist.");
        }

        if (city.CountryId != request.CountryId)
        {
            throw new InvalidOperationException(
                "The selected city does not belong to the selected country.");
        }

        if (await _warehouses.AnyDuplicateNameAsync(name, null, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A warehouse named '{name}' already exists.");
        }

        var warehouse = new Warehouse
        {
            Name = name,
            Address = address,
            CityId = city.Id,
            CountryId = city.CountryId,
        };

        await _warehouses.AddAsync(warehouse, cancellationToken);
        await _warehouses.SaveChangesAsync(cancellationToken);

        warehouse.City = city;
        warehouse.Country = city.Country;

        return ToDto(warehouse);
    }

    public async Task<WarehouseDto> UpdateAsync(
        int id,
        WarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouses.GetForUpdateAsync(id, cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException("The requested warehouse was not found.");
        }

        var name = Require(request.Name, "Warehouse name");
        var address = Require(request.Address, "Address");

        var city = await _warehouses.GetCityWithCountryAsync(request.CityId, cancellationToken);

        if (city is null)
        {
            throw new InvalidOperationException("The selected city does not exist.");
        }

        if (city.CountryId != request.CountryId)
        {
            throw new InvalidOperationException(
                "The selected city does not belong to the selected country.");
        }

        if (await _warehouses.AnyDuplicateNameAsync(name, id, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A warehouse named '{name}' already exists.");
        }

        warehouse.Name = name;
        warehouse.Address = address;
        warehouse.CityId = city.Id;
        warehouse.CountryId = city.CountryId;

        await _warehouses.SaveChangesAsync(cancellationToken);

        warehouse.City = city;
        warehouse.Country = city.Country;

        return ToDto(warehouse);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouses.GetByIdAsync(id, cancellationToken);

        if (warehouse is null)
        {
            throw new KeyNotFoundException("The requested warehouse was not found.");
        }

        _warehouses.Remove(warehouse);
        await _warehouses.SaveChangesAsync(cancellationToken);
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
        ItemCount = warehouse.Items?.Count ?? 0,
    };
}
