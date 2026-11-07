using Bariza.Application.Common;
using Bariza.Application.DTOs.Marketplace;
using Bariza.Application.Interfaces.Repositories;
using Bariza.Application.Models;
using Bariza.Core.Entities;
using Bariza.Core.Enums;
using Bariza.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bariza.Infrastructure.Repositories;

public class MarketplaceCatalogRepository : IMarketplaceCatalogRepository
{
    private readonly AppDbContext _db;

    public MarketplaceCatalogRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<MarketplaceProductDto> Items, int TotalCount)> GetProductsAsync(
        string? search,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        MarketplaceProductSort sort,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = PublicProductsQuery();

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        var term = search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(product =>
                product.Name.ToLower().Contains(term) ||
                (product.Description != null && product.Description.ToLower().Contains(term)) ||
                (product.Sku != null && product.Sku.ToLower().Contains(term)));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(product =>
                (product.DiscountPrice ?? product.Price) >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(product =>
                (product.DiscountPrice ?? product.Price) <= maxPrice.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = sort switch
        {
            MarketplaceProductSort.PriceAscending => query
                .OrderBy(product => product.DiscountPrice ?? product.Price)
                .ThenBy(product => product.Id),
            MarketplaceProductSort.PriceDescending => query
                .OrderByDescending(product => product.DiscountPrice ?? product.Price)
                .ThenBy(product => product.Id),
            MarketplaceProductSort.NameAscending => query
                .OrderBy(product => product.Name.ToLower())
                .ThenBy(product => product.Id),
            _ => query
                .OrderByDescending(product => product.CreatedUtc)
                .ThenByDescending(product => product.Id),
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new MarketplaceProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Sku = product.Sku,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                CategoryName = product.Category != null ? product.Category.Name : string.Empty,
                SellerName = product.SellerProfile != null
                    ? product.SellerProfile.StoreName
                    : null,
                IsAvailable = product.WarehouseItems.Any(item => item.Qty > 0),
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<MarketplaceCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        // Count only products that are visible in the public catalog. Categories
        // without any visible product are omitted so the filter never leads to a
        // guaranteed-empty result.
        return await _db.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new MarketplaceCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ProductCount = category.Products.Count(product =>
                    product.IsPublished &&
                    product.IsActive &&
                    product.ApprovalStatus == ProductApprovalStatus.Approved &&
                    (product.SellerProfile == null ||
                     (product.SellerProfile.IsApproved && product.SellerProfile.IsActive))),
            })
            .Where(category => category.ProductCount > 0)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetailsDto?> GetProductDetailsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.SellerProfile)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var related = await GetRelatedProductsInternalAsync(product, cancellationToken);
        var stock = product.WarehouseItems.Sum(w => w.Qty);
        var summary = await GetReviewSummaryInternalAsync(productId, cancellationToken);

        return MapProductDetails(product, stock, summary, related);
    }

    public async Task<ProductDetailsDto?> GetProductDetailsBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.SellerProfile)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var related = await GetRelatedProductsInternalAsync(product, cancellationToken);
        var stock = product.WarehouseItems.Sum(w => w.Qty);
        var summary = await GetReviewSummaryInternalAsync(product.Id, cancellationToken);

        return MapProductDetails(product, stock, summary, related);
    }

    public async Task<IReadOnlyList<MarketplaceProductDto>> GetRelatedProductsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return Array.Empty<MarketplaceProductDto>();
        }

        return await GetRelatedProductsInternalAsync(product, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReviewDto>> GetReviewsAsync(int productId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 60);

        return await _db.ProductReviews
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User != null ? r.User.FullName : string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedUtc = r.CreatedUtc,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ReviewSummaryDto> GetReviewSummaryAsync(int productId, CancellationToken cancellationToken = default) =>
        await GetReviewSummaryInternalAsync(productId, cancellationToken);

    public async Task AddReviewAsync(int userId, int productId, int rating, string? comment, CancellationToken cancellationToken = default)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ValidationException("Rating must be between 1 and 5.");
        }

        var exists = await _db.ProductReviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId, cancellationToken);
        if (exists)
        {
            throw new ConflictException("You have already reviewed this product.");
        }

        _db.ProductReviews.Add(new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Comment = comment,
            CreatedUtc = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WishlistItemDto>> GetWishlistAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.WishlistItems
            .AsNoTracking()
            .Include(w => w.Product)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedUtc)
            .Select(w => new WishlistItemDto
            {
                Id = w.Id,
                ProductId = w.ProductId,
                ProductName = w.Product != null ? w.Product.Name : string.Empty,
                ImageUrl = w.Product != null ? w.Product.ImageUrl : null,
                Price = w.Product != null ? (w.Product.DiscountPrice ?? w.Product.Price) : 0m,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task AddToWishlistAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
        if (exists)
        {
            return;
        }

        _db.WishlistItems.Add(new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            CreatedUtc = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFromWishlistAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
        if (item is not null)
        {
            _db.WishlistItems.Remove(item);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyList<MarketplaceProductDto>> GetRelatedProductsInternalAsync(Product product, CancellationToken cancellationToken)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p =>
                p.Id != product.Id &&
                p.IsPublished &&
                p.IsActive &&
                p.ApprovalStatus == ProductApprovalStatus.Approved &&
                (p.CategoryId == product.CategoryId ||
                 p.SellerProfileId == product.SellerProfileId))
            .OrderByDescending(p => p.CreatedUtc)
            .Take(8)
            .Select(p => new MarketplaceProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Sku = p.Sku,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                SellerName = p.SellerProfile != null ? p.SellerProfile.StoreName : null,
                IsAvailable = p.WarehouseItems.Any(w => w.Qty > 0),
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<ReviewSummaryDto> GetReviewSummaryInternalAsync(int productId, CancellationToken cancellationToken)
    {
        var reviews = await _db.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
            .ToListAsync(cancellationToken);

        var summary = new ReviewSummaryDto
        {
            TotalReviews = reviews.Count,
            AverageRating = reviews.Count == 0 ? 0 : reviews.Average(r => r.Rating),
        };

        for (var rating = 1; rating <= 5; rating++)
        {
            summary.Distribution[rating] = reviews.Count(r => r.Rating == rating);
        }

        return summary;
    }

    private static ProductDetailsDto MapProductDetails(
        Product product,
        int stock,
        ReviewSummaryDto summary,
        IReadOnlyList<MarketplaceProductDto> related)
    {
        return new ProductDetailsDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            Description = product.Description,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            SellerProfileId = product.SellerProfileId,
            SellerName = product.SellerProfile?.StoreName,
            IsAvailable = stock > 0,
            StockQuantity = stock,
            AverageRating = summary.AverageRating,
            ReviewCount = summary.TotalReviews,
            Images = product.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    Url = i.Url,
                    SortOrder = i.SortOrder,
                    IsPrimary = i.IsPrimary,
                })
                .ToList(),
            Variants = product.Variants
                .Where(v => v.IsActive)
                .Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Name = v.Name,
                    Value = v.Value,
                    PriceOverride = v.PriceOverride,
                    Stock = v.Stock,
                    IsActive = v.IsActive,
                })
                .ToList(),
            Specifications = product.Specifications
                .OrderBy(s => s.SortOrder)
                .Select(s => new ProductSpecificationDto
                {
                    Id = s.Id,
                    Key = s.Key,
                    Value = s.Value,
                    SortOrder = s.SortOrder,
                })
                .ToList(),
            RelatedProducts = related,
        };
    }

    private IQueryable<Product> PublicProductsQuery()
    {
        return _db.Products
            .AsNoTracking()
            .Where(product =>
                product.IsPublished &&
                product.IsActive &&
                product.ApprovalStatus == ProductApprovalStatus.Approved &&
                product.Category != null &&
                product.Category.IsActive &&
                (product.SellerProfile == null ||
                 (product.SellerProfile.IsApproved && product.SellerProfile.IsActive)));
    }
}
