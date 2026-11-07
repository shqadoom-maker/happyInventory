using System.ComponentModel.DataAnnotations;

namespace Bariza.Core.Entities;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsPrimary { get; set; }
}

public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;

    public decimal? PriceOverride { get; set; }

    public int Stock { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ProductSpecification
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    [Required]
    [MaxLength(120)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Value { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

public class ProductReview
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class WishlistItem
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
