using Bariza.Application.Common;
using Bariza.Application.DTOs.Marketplace;

namespace Bariza.Application.Interfaces.Services;

public interface IMarketplaceCatalogService
{
    Task<PagedResponse<MarketplaceProductDto>> GetProductsAsync(
        MarketplaceProductQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketplaceCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);
}
