using happyInventory.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace happyInventory.Data.Configurations;

public class WarehouseItemConfiguration : IEntityTypeConfiguration<WarehouseItem>
{
    public void Configure(EntityTypeBuilder<WarehouseItem> builder)
    {
        builder.HasIndex(item => new { item.WarehouseId, item.ItemName }).IsUnique();

        builder.HasOne(item => item.Warehouse)
            .WithMany(warehouse => warehouse.Items)
            .HasForeignKey(item => item.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
