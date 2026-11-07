using Bariza.Application.Models;

namespace Bariza.Application.Interfaces.Repositories;

public interface IDashboardRepository
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
