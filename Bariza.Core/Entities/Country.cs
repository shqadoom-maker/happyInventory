using System.ComponentModel.DataAnnotations;

namespace Bariza.Core.Entities;

public class Country
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    public ICollection<City> Cities { get; set; } = new List<City>();
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
