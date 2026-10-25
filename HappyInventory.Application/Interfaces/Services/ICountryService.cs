using HappyInventory.Application.DTOs.Countries;

namespace HappyInventory.Application.Interfaces.Services;

public interface ICountryService
{
    Task<IReadOnlyList<CountryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CountryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CountryDto> CreateAsync(CountryRequest request, CancellationToken cancellationToken = default);
    Task<CountryDto> UpdateAsync(int id, CountryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
