using System.ComponentModel.DataAnnotations;

namespace HappyInventory.Core.Entities;

public class SellerProfile
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string StoreName { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsApproved { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
