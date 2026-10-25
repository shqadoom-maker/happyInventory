using happyInventory.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace happyInventory.Data.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasIndex(warehouse => warehouse.Name).IsUnique();

        builder.HasOne(warehouse => warehouse.City)
            .WithMany(city => city.Warehouses)
            .HasForeignKey(warehouse => warehouse.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(warehouse => warehouse.Country)
            .WithMany(country => country.Warehouses)
            .HasForeignKey(warehouse => warehouse.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
