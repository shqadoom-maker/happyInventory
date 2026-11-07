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
