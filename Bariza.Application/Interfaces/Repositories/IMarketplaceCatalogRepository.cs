using Bariza.Application.DTOs.Marketplace;
using Bariza.Application.Models;

namespace Bariza.Application.Interfaces.Repositories;

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
