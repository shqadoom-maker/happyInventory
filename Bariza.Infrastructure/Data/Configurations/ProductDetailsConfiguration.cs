using Bariza.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bariza.Infrastructure.Data.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.SortOrder });
        builder.HasOne(x => x.Product).WithMany(p => p.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PriceOverride).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.ProductId);
        builder.HasOne(x => x.Product).WithMany(p => p.Variants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductSpecificationConfiguration : IEntityTypeConfiguration<ProductSpecification>
{
    public void Configure(EntityTypeBuilder<ProductSpecification> builder)
    {
        builder.ToTable("ProductSpecifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.SortOrder });
        builder.HasOne(x => x.Product).WithMany(p => p.Specifications).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Rating).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.UserId }).IsUnique();
        builder.HasOne(x => x.Product).WithMany(p => p.Reviews).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedUtc).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
