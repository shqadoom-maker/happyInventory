using HappyInventory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HappyInventory.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(product => product.Slug)
            .HasMaxLength(220)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(2000);

        builder.Property(product => product.Sku)
            .HasMaxLength(50);

        builder.Property(product => product.ImageUrl)
            .HasMaxLength(500);

        // SQLite compares and orders decimal columns as text, which is not
        // correct for money. Persist prices as integer cents and expose decimals
        // through the entity/DTO so filtering and sorting happen numerically.
        var priceConverter = new ValueConverter<decimal, long>(
            value => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero),
            value => value / 100m);

        builder.Property(product => product.Price)
            .HasConversion(priceConverter)
            .HasColumnType("INTEGER")
            .IsRequired();

        builder.Property(product => product.DiscountPrice)
            .HasConversion(priceConverter)
            .HasColumnType("INTEGER");

        builder.HasIndex(product => product.Slug).IsUnique();
        builder.HasIndex(product => product.Sku);
        builder.HasIndex(product => new { product.IsPublished, product.IsActive, product.ApprovalStatus });
        builder.HasIndex(product => product.CategoryId);
        builder.HasIndex(product => product.SellerProfileId);
        builder.HasIndex(product => product.CreatedUtc);

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(product => product.SellerProfile)
            .WithMany(seller => seller.Products)
            .HasForeignKey(product => product.SellerProfileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
