using System.ComponentModel.DataAnnotations;

namespace HappyInventory.Application.DTOs.Marketplace;

public class MarketplaceProductQuery
{
    [MaxLength(200)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal? MinPrice { get; set; }

    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal? MaxPrice { get; set; }

    [MaxLength(20)]
    public string? SortBy { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 60)]
    public int PageSize { get; set; } = 12;
}
