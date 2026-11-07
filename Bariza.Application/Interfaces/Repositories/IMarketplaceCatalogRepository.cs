using Bariza.Application.Common;
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

    Task<ProductDetailsDto?> GetProductDetailsAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<ProductDetailsDto?> GetProductDetailsBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MarketplaceProductDto>> GetRelatedProductsAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReviewDto>> GetReviewsAsync(
        int productId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ReviewSummaryDto> GetReviewSummaryAsync(
        int productId,
        CancellationToken cancellationToken = default);

    Task AddReviewAsync(
        int userId,
        int productId,
        int rating,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WishlistItemDto>> GetWishlistAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task AddToWishlistAsync(
        int userId,
        int productId,
        CancellationToken cancellationToken = default);

    Task RemoveFromWishlistAsync(
        int userId,
        int productId,
        CancellationToken cancellationToken = default);
}
