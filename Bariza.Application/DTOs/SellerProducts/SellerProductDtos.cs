using Bariza.Core.Entities;
using Bariza.Core.Enums;

namespace Bariza.Application.DTOs.SellerProducts;

public class SellerProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; }
    public ProductApprovalStatus ApprovalStatus { get; set; }
}

public class CreateSellerProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public bool IsPublished { get; set; }
}

public class UpdateSellerProductRequest : CreateSellerProductRequest
{
    public bool IsActive { get; set; } = true;
}
