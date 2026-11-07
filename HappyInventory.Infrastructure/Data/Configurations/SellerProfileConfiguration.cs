using HappyInventory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HappyInventory.Infrastructure.Data.Configurations;

public class SellerProfileConfiguration : IEntityTypeConfiguration<SellerProfile>
{
    public void Configure(EntityTypeBuilder<SellerProfile> builder)
    {
        builder.ToTable("SellerProfiles");
        builder.HasKey(seller => seller.Id);

        builder.Property(seller => seller.StoreName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(seller => seller.Slug)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(seller => seller.Description)
            .HasMaxLength(500);

        builder.HasIndex(seller => seller.Slug).IsUnique();
        builder.HasIndex(seller => new { seller.IsApproved, seller.IsActive });
    }
}
