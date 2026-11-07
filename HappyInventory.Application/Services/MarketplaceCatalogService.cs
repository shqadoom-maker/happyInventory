using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Marketplace;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Application.Models;

namespace HappyInventory.Application.Services;

public class MarketplaceCatalogService : IMarketplaceCatalogService
{
    private const int MaxPageSize = 60;
    private readonly IMarketplaceCatalogRepository _catalog;

    public MarketplaceCatalogService(IMarketplaceCatalogRepository catalog)
    {
        _catalog = catalog;
    }

    public async Task<PagedResponse<MarketplaceProductDto>> GetProductsAsync(
        MarketplaceProductQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.PageNumber < 1)
        {
            throw new ValidationException("pageNumber must be greater than zero.");
        }

        if (query.PageSize < 1 || query.PageSize > MaxPageSize)
        {
            throw new ValidationException($"pageSize must be between 1 and {MaxPageSize}.");
        }

        if (query.MinPrice.HasValue && query.MaxPrice.HasValue &&
            query.MinPrice.Value > query.MaxPrice.Value)
        {
            throw new ValidationException(
                "The minimum price cannot be greater than the maximum price.");
        }

        var sort = ParseSort(query.SortBy);

        var (items, totalCount) = await _catalog.GetProductsAsync(
            query.Search,
            query.CategoryId,
            query.MinPrice,
            query.MaxPrice,
            sort,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return new PagedResponse<MarketplaceProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)query.PageSize),
        };
    }

    public Task<IReadOnlyList<MarketplaceCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return _catalog.GetCategoriesAsync(cancellationToken);
    }

    private static MarketplaceProductSort ParseSort(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return MarketplaceProductSort.Newest;
        }

        return sortBy.Trim().ToLowerInvariant() switch
        {
            "newest" => MarketplaceProductSort.Newest,
            "priceasc" => MarketplaceProductSort.PriceAscending,
            "pricedesc" => MarketplaceProductSort.PriceDescending,
            "nameasc" => MarketplaceProductSort.NameAscending,
            _ => throw new ValidationException(
                "sortBy must be one of: newest, priceAsc, priceDesc, nameAsc."),
        };
    }
}
