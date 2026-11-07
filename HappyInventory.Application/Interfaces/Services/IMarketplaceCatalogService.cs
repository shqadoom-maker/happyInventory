using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Marketplace;

namespace HappyInventory.Application.Interfaces.Services;

public interface IMarketplaceCatalogService
{
    Task<PagedResponse<MarketplaceProductDto>> GetProductsAsync(
        MarketplaceProductQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketplaceCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);
}
