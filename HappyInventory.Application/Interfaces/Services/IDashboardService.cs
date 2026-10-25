using HappyInventory.Application.DTOs.Dashboard;

namespace HappyInventory.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
