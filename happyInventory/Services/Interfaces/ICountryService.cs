using happyInventory.DTOs.Countries;

namespace happyInventory.Services.Interfaces;

public interface ICountryService
{
    Task<IReadOnlyList<CountryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CountryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CountryDto> CreateAsync(CountryRequest request, CancellationToken cancellationToken = default);
    Task<CountryDto> UpdateAsync(int id, CountryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
