using happyInventory.DTOs.Dashboard;

namespace happyInventory.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
