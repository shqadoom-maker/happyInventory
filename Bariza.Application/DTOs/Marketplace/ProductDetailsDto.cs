namespace Bariza.Application.DTOs.Marketplace;

public class ProductImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class ProductVariantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public decimal? PriceOverride { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}

public class ProductSpecificationDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class ProductReviewDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class ReviewSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> Distribution { get; set; } = new();
}

public class ProductDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? SellerProfileId { get; set; }
    public string? SellerName { get; set; }
    public string? SellerLogoUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int StockQuantity { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public IReadOnlyList<ProductImageDto> Images { get; set; } = Array.Empty<ProductImageDto>();
    public IReadOnlyList<ProductVariantDto> Variants { get; set; } = Array.Empty<ProductVariantDto>();
    public IReadOnlyList<ProductSpecificationDto> Specifications { get; set; } = Array.Empty<ProductSpecificationDto>();
    public IReadOnlyList<MarketplaceProductDto> RelatedProducts { get; set; } = Array.Empty<MarketplaceProductDto>();
}

public class WishlistItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
}
