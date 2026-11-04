using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Cities;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Services;

public class CityService : ICityService
{
    private readonly ICityRepository _cities;

    public CityService(ICityRepository cities)
    {
        _cities = cities;
    }

    public async Task<IReadOnlyList<CityDto>> GetAllAsync(
        int? countryId,
        CancellationToken cancellationToken = default)
    {
        var cities = await _cities.GetAllAsync(countryId, cancellationToken);
        return cities.Select(ToDto).ToList();
    }

    public async Task<CityDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var city = await _cities.GetByIdAsync(id, cancellationToken);

        if (city is null)
        {
            throw new NotFoundException("The requested city was not found.");
        }

        return ToDto(city);
    }

    public async Task<CityDto> CreateAsync(
        CityRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = Require(request.Name);

        if (!await _cities.CountryExistsAsync(request.CountryId, cancellationToken))
        {
            throw new ValidationException("The selected country does not exist.");
        }

        if (await _cities.AnyDuplicateAsync(name, request.CountryId, null, cancellationToken))
        {
            throw new ConflictException(
                "A city with the same name already exists in that country.");
        }

        var city = new City
        {
            Name = name,
            CountryId = request.CountryId,
        };

        await _cities.AddAsync(city, cancellationToken);
        await _cities.SaveChangesAsync(cancellationToken);

        // Return the DTO directly without an extra round-trip to the database.
        // CountryName is intentionally left empty here; callers can refresh the
        // city list if they need the related country name.
        return new CityDto
        {
            Id = city.Id,
            Name = city.Name,
            CountryId = city.CountryId,
            CountryName = string.Empty,
        };
    }

    public async Task<CityDto> UpdateAsync(
        int id,
        CityRequest request,
        CancellationToken cancellationToken = default)
    {
        var city = await _cities.GetByIdAsync(id, cancellationToken);

        if (city is null)
        {
            throw new NotFoundException("The requested city was not found.");
        }

        var name = Require(request.Name);

        if (!await _cities.CountryExistsAsync(request.CountryId, cancellationToken))
        {
            throw new ValidationException("The selected country does not exist.");
        }

        if (await _cities.AnyDuplicateAsync(name, request.CountryId, id, cancellationToken))
        {
            throw new ConflictException(
                "A city with the same name already exists in that country.");
        }

        city.Name = name;
        city.CountryId = request.CountryId;

        await _cities.SaveChangesAsync(cancellationToken);

        // Return the DTO directly without an extra round-trip to the database.
        // CountryName is intentionally left empty here; callers can refresh the
        // city list if they need the related country name.
        return new CityDto
        {
            Id = city.Id,
            Name = city.Name,
            CountryId = city.CountryId,
            CountryName = string.Empty,
        };
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var city = await _cities.GetByIdAsync(id, cancellationToken);

        if (city is null)
        {
            throw new NotFoundException("The requested city was not found.");
        }

        if (await _cities.IsInUseAsync(id, cancellationToken))
        {
            throw new ConflictException(
                "The city cannot be deleted while it has warehouses.");
        }

        _cities.Remove(city);
        await _cities.SaveChangesAsync(cancellationToken);
    }

    private static string Require(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ValidationException("City name is required.");
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
