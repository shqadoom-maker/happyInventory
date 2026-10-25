using HappyInventory.Application.DTOs.Countries;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Services;

public class CountryService : ICountryService
{
    private readonly ICountryRepository _countries;

    public CountryService(ICountryRepository countries)
    {
        _countries = countries;
    }

    public async Task<IReadOnlyList<CountryDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var countries = await _countries.GetAllAsync(cancellationToken);
        return countries.Select(ToDto).ToList();
    }

    public async Task<CountryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var country = await _countries.GetByIdAsync(id, cancellationToken);

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

        if (await _countries.AnyDuplicateAsync(name, code, null, cancellationToken))
        {
            throw new InvalidOperationException(
                "A country with the same name or code already exists.");
        }

        var country = new Country
        {
            Name = name,
            Code = code,
        };

        await _countries.AddAsync(country, cancellationToken);
        await _countries.SaveChangesAsync(cancellationToken);

        return ToDto(country);
    }

    public async Task<CountryDto> UpdateAsync(
        int id,
        CountryRequest request,
        CancellationToken cancellationToken = default)
    {
        var country = await _countries.GetByIdAsync(id, cancellationToken);

        if (country is null)
        {
            throw new KeyNotFoundException("The requested country was not found.");
        }

        var name = Require(request.Name, "Country name");
        var code = Require(request.Code, "Country code").ToUpperInvariant();

        if (await _countries.AnyDuplicateAsync(name, code, id, cancellationToken))
        {
            throw new InvalidOperationException(
                "A country with the same name or code already exists.");
        }

        country.Name = name;
        country.Code = code;

        await _countries.SaveChangesAsync(cancellationToken);

        return ToDto(country);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var country = await _countries.GetByIdAsync(id, cancellationToken);

        if (country is null)
        {
            throw new KeyNotFoundException("The requested country was not found.");
        }

        if (await _countries.IsInUseAsync(id, cancellationToken))
        {
            throw new InvalidOperationException(
                "The country cannot be deleted while it has cities or warehouses.");
        }

        _countries.Remove(country);
        await _countries.SaveChangesAsync(cancellationToken);
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
