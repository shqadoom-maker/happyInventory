using HappyInventory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyInventory.Infrastructure.Data.Configurations;

public class WarehouseItemConfiguration : IEntityTypeConfiguration<WarehouseItem>
{
    public void Configure(EntityTypeBuilder<WarehouseItem> builder)
    {
        builder.HasIndex(item => new { item.WarehouseId, item.ItemName }).IsUnique();
        builder.HasIndex(item => item.WarehouseId);
        builder.HasIndex(item => item.SKUCode);
        builder.HasIndex(item => item.Qty);
        builder.HasIndex(item => item.ProductId);

        builder.HasOne(item => item.Warehouse)
            .WithMany(warehouse => warehouse.Items)
            .HasForeignKey(item => item.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.WarehouseItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
