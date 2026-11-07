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
