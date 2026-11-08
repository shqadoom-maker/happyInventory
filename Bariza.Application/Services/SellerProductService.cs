using Bariza.Application.Common;
using Bariza.Application.DTOs.SellerProducts;
using Bariza.Application.Interfaces.Repositories;
using Bariza.Application.Interfaces.Services;
using Bariza.Core.Entities;
using Bariza.Core.Enums;

namespace Bariza.Application.Services;

public class SellerProductService : ISellerProductService
{
    private readonly ISellerProductRepository _repo;

    public SellerProductService(ISellerProductRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<SellerProductDto>> GetProductsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var seller = await GetSellerProfileAsync(userId, cancellationToken);
        var products = await _repo.GetProductsAsync(seller.Id, cancellationToken);
        return products.Select(Map).ToList();
    }

    public async Task<SellerProductDto> CreateProductAsync(int userId, CreateSellerProductRequest request, CancellationToken cancellationToken = default)
    {
        var seller = await GetSellerProfileAsync(userId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Product name is required.");
        }

        if (request.Price < 0)
        {
            throw new ValidationException("Price cannot be negative.");
        }

        var category = (await _repo.GetCategoriesAsync(cancellationToken)).FirstOrDefault(c => c.Id == request.CategoryId);
        if (category is null)
        {
            throw new ValidationException("Category is required.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? request.Name.Trim().ToLowerInvariant().Replace(' ', '-') : request.Slug.Trim(),
            Description = request.Description,
            Sku = request.Sku,
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            SellerProfileId = seller.Id,
            IsPublished = request.IsPublished,
            IsActive = true,
            ApprovalStatus = ProductApprovalStatus.Draft,
            CreatedUtc = DateTime.UtcNow,
        };

        await _repo.AddProductAsync(product, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        product.Category = category;
        product.SellerProfile = seller;
        return Map(product);
    }

    public async Task<SellerProductDto> UpdateProductAsync(int userId, int productId, UpdateSellerProductRequest request, CancellationToken cancellationToken = default)
    {
        var seller = await GetSellerProfileAsync(userId, cancellationToken);
        var product = await _repo.GetProductAsync(seller.Id, productId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException("The requested product was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Product name is required.");
        }

        var category = (await _repo.GetCategoriesAsync(cancellationToken)).FirstOrDefault(c => c.Id == request.CategoryId);
        if (category is null)
        {
            throw new ValidationException("Category is required.");
        }

        product.Name = request.Name.Trim();
        product.Slug = string.IsNullOrWhiteSpace(request.Slug) ? request.Name.Trim().ToLowerInvariant().Replace(' ', '-') : request.Slug.Trim();
        product.Description = request.Description;
        product.Sku = request.Sku;
        product.Price = request.Price;
        product.DiscountPrice = request.DiscountPrice;
        product.ImageUrl = request.ImageUrl;
        product.CategoryId = request.CategoryId;
        product.Category = category;
        product.IsPublished = request.IsPublished;
        product.IsActive = request.IsActive;
        product.UpdatedUtc = DateTime.UtcNow;

        await _repo.UpdateProductAsync(product, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        return Map(product);
    }

    public async Task DeleteProductAsync(int userId, int productId, CancellationToken cancellationToken = default)
    {
        var seller = await GetSellerProfileAsync(userId, cancellationToken);
        var product = await _repo.GetProductAsync(seller.Id, productId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException("The requested product was not found.");
        }

        await _repo.DeleteProductAsync(product, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SellerProductDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _repo.GetCategoriesAsync(cancellationToken);
        return categories.Select(c => new SellerProductDto
        {
            Id = c.Id,
            Name = c.Name,
            CategoryId = c.Id,
            CategoryName = c.Name,
        }).ToList();
    }

    private async Task<SellerProfile> GetSellerProfileAsync(int userId, CancellationToken cancellationToken)
    {
        var seller = await _repo.GetSellerProfileByUserIdAsync(userId, cancellationToken);
        if (seller is not null)
        {
            return seller;
        }

        seller = new SellerProfile
        {
            UserId = userId,
            StoreName = $"Seller {userId}",
            Slug = $"seller-{userId}",
            IsApproved = true,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow,
        };

        await _repo.AddSellerProfileAsync(seller, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);
        return seller;
    }

    private static SellerProductDto Map(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Slug = product.Slug,
        Description = product.Description,
        Sku = product.Sku,
        Price = product.Price,
        DiscountPrice = product.DiscountPrice,
        ImageUrl = product.ImageUrl,
        CategoryId = product.CategoryId,
        CategoryName = product.Category?.Name ?? string.Empty,
        IsPublished = product.IsPublished,
        IsActive = product.IsActive,
        ApprovalStatus = product.ApprovalStatus,
    };
}
