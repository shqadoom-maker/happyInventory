using Bariza.Application.Common;
using Bariza.Application.DTOs.Marketplace;
using Bariza.Application.Interfaces.Repositories;
using Bariza.Application.Interfaces.Services;
using Bariza.Application.Models;

namespace Bariza.Application.Services;

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

    public Task<IReadOnlyList<MarketplaceCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        _catalog.GetCategoriesAsync(cancellationToken);

    public async Task<ProductDetailsDto?> GetProductDetailsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var details = await _catalog.GetProductDetailsAsync(productId, cancellationToken);
        if (details is null)
        {
            return null;
        }

        details.RelatedProducts = await _catalog.GetRelatedProductsAsync(productId, cancellationToken);
        return details;
    }

    public Task<ProductDetailsDto?> GetProductDetailsBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        _catalog.GetProductDetailsBySlugAsync(slug, cancellationToken);

    public Task<IReadOnlyList<MarketplaceProductDto>> GetRelatedProductsAsync(int productId, CancellationToken cancellationToken = default) =>
        _catalog.GetRelatedProductsAsync(productId, cancellationToken);

    public Task<IReadOnlyList<ProductReviewDto>> GetReviewsAsync(int productId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        _catalog.GetReviewsAsync(productId, page, pageSize, cancellationToken);

    public Task<ReviewSummaryDto> GetReviewSummaryAsync(int productId, CancellationToken cancellationToken = default) =>
        _catalog.GetReviewSummaryAsync(productId, cancellationToken);

    public async Task AddReviewAsync(int userId, int productId, int rating, string? comment, CancellationToken cancellationToken = default)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ValidationException("Rating must be between 1 and 5.");
        }

        await _catalog.AddReviewAsync(userId, productId, rating, comment, cancellationToken);
    }

    public Task<IReadOnlyList<WishlistItemDto>> GetWishlistAsync(int userId, CancellationToken cancellationToken = default) =>
        _catalog.GetWishlistAsync(userId, cancellationToken);

    public Task AddToWishlistAsync(int userId, int productId, CancellationToken cancellationToken = default) =>
        _catalog.AddToWishlistAsync(userId, productId, cancellationToken);

    public Task RemoveFromWishlistAsync(int userId, int productId, CancellationToken cancellationToken = default) =>
        _catalog.RemoveFromWishlistAsync(userId, productId, cancellationToken);

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
