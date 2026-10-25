using System.ComponentModel.DataAnnotations;

namespace happyInventory.Models;

public class Warehouse
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public int CityId { get; set; }

    public City? City { get; set; }

    public int CountryId { get; set; }

    public Country? Country { get; set; }

    public ICollection<WarehouseItem> Items { get; set; } = new List<WarehouseItem>();
}
