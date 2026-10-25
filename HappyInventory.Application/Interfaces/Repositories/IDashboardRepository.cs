using HappyInventory.Application.Models;

namespace HappyInventory.Application.Interfaces.Repositories;

public interface IDashboardRepository
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
