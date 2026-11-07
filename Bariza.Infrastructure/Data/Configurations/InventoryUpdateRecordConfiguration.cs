using Bariza.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bariza.Infrastructure.Data.Configurations;

public class InventoryUpdateRecordConfiguration : IEntityTypeConfiguration<InventoryUpdateRecord>
{
    public void Configure(EntityTypeBuilder<InventoryUpdateRecord> builder)
    {
        builder.HasKey(record => record.IdempotencyKey);

        builder.Property(record => record.IdempotencyKey)
            .HasMaxLength(100);

        builder.Property(record => record.SKUCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(record => record.ItemName)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(record => record.CreatedUtc);
        builder.HasIndex(record => record.WarehouseId);
        builder.HasIndex(record => record.SKUCode);
    }
}
