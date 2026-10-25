using System.ComponentModel.DataAnnotations;

namespace HappyInventory.Application.DTOs.Cities;

public class CityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string? CountryName { get; set; }
}

public class CityRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CountryId { get; set; }
}
