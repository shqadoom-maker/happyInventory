using Bariza.Application.DTOs.Dashboard;

namespace Bariza.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
