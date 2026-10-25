using HappyInventory.Application.DTOs.Cities;

namespace HappyInventory.Application.Interfaces.Services;

public interface ICityService
{
    Task<IReadOnlyList<CityDto>> GetAllAsync(int? countryId, CancellationToken cancellationToken = default);
    Task<CityDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CityDto> CreateAsync(CityRequest request, CancellationToken cancellationToken = default);
    Task<CityDto> UpdateAsync(int id, CityRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
