using System.ComponentModel.DataAnnotations;

namespace happyInventory.Models;

public class City
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int CountryId { get; set; }

    public Country? Country { get; set; }

    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
