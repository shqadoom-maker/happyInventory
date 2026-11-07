using HappyInventory.Application.DTOs.Marketplace;
using HappyInventory.Application.Models;

namespace HappyInventory.Application.Interfaces.Repositories;

public interface IMarketplaceCatalogRepository
{
    Task<(IReadOnlyList<MarketplaceProductDto> Items, int TotalCount)> GetProductsAsync(
        string? search,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        MarketplaceProductSort sort,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketplaceCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);
}
