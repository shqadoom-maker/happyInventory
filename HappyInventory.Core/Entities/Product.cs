using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HappyInventory.Core.Enums;

namespace HappyInventory.Core.Entities;

public class Product
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(220)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Sku { get; set; }

    [Required]
    [Range(typeof(decimal), "0", "1000000000000")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "1000000000000")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPrice { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public int? SellerProfileId { get; set; }

    public SellerProfile? SellerProfile { get; set; }

    public bool IsPublished { get; set; }

    public bool IsActive { get; set; } = true;

    public ProductApprovalStatus ApprovalStatus { get; set; } = ProductApprovalStatus.Draft;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedUtc { get; set; }

    public ICollection<WarehouseItem> WarehouseItems { get; set; } = new List<WarehouseItem>();
}
