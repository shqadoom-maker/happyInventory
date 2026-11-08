using Bariza.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Bariza.Infrastructure.Data.Configurations;

public class ShippingAddressConfiguration : IEntityTypeConfiguration<ShippingAddress>
{
    public void Configure(EntityTypeBuilder<ShippingAddress> builder)
    {
        builder.ToTable("ShippingAddresses");
        builder.HasKey(address => address.Id);

        builder.Property(address => address.FullName).HasMaxLength(150).IsRequired();
        builder.Property(address => address.Phone).HasMaxLength(30).IsRequired();
        builder.Property(address => address.Country).HasMaxLength(100).IsRequired();
        builder.Property(address => address.City).HasMaxLength(100).IsRequired();
        builder.Property(address => address.Address).HasMaxLength(500).IsRequired();
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderNumber).HasMaxLength(40).IsRequired();
        builder.Property(order => order.CheckoutReference).HasMaxLength(60).IsRequired();
        builder.Property(order => order.TotalAmount).HasConversion(MoneyConverter()).HasColumnType("INTEGER").IsRequired();
        builder.Property(order => order.CreatedUtc).IsRequired();
        builder.Property(order => order.UpdatedUtc);

        builder.HasIndex(order => order.OrderNumber).IsUnique();
        builder.HasIndex(order => order.UserId);
        builder.HasIndex(order => order.CreatedUtc);

        builder.HasOne(order => order.User)
            .WithMany()
            .HasForeignKey(order => order.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.ShippingAddress)
            .WithMany(address => address.Orders)
            .HasForeignKey(order => order.ShippingAddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static ValueConverter<decimal, long> MoneyConverter() =>
        new(
            value => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero),
            value => value / 100m);
}

public class SellerOrderConfiguration : IEntityTypeConfiguration<SellerOrder>
{
    public void Configure(EntityTypeBuilder<SellerOrder> builder)
    {
        builder.ToTable("SellerOrders");
        builder.HasKey(order => order.Id);

        builder.Property(order => order.TotalAmount).HasConversion(MoneyConverter()).HasColumnType("INTEGER").IsRequired();
        builder.Property(order => order.CreatedUtc).IsRequired();
        builder.Property(order => order.UpdatedUtc);

        builder.HasIndex(order => order.OrderId);
        builder.HasIndex(order => order.SellerProfileId);

        builder.HasOne(order => order.Order)
            .WithMany(parent => parent.SellerOrders)
            .HasForeignKey(order => order.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(order => order.SellerProfile)
            .WithMany()
            .HasForeignKey(order => order.SellerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static ValueConverter<decimal, long> MoneyConverter() =>
        new(
            value => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero),
            value => value / 100m);
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.UnitPrice).HasConversion(MoneyConverter()).HasColumnType("INTEGER").IsRequired();
        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.Subtotal).HasConversion(MoneyConverter()).HasColumnType("INTEGER").IsRequired();

        builder.HasOne(item => item.SellerOrder)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.SellerOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(item => item.WarehouseItem)
            .WithMany()
            .HasForeignKey(item => item.WarehouseItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static ValueConverter<decimal, long> MoneyConverter() =>
        new(
            value => (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero),
            value => value / 100m);
}

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(cart => cart.Id);

        builder.Property(cart => cart.CreatedUtc).IsRequired();
        builder.Property(cart => cart.UpdatedUtc);

        builder.HasIndex(cart => cart.UserId).IsUnique();

        builder.HasOne(cart => cart.User)
            .WithMany()
            .HasForeignKey(cart => cart.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.CreatedUtc).IsRequired();
        builder.Property(item => item.UpdatedUtc);

        builder.HasIndex(item => new { item.CartId, item.ProductId, item.ProductVariantId }).IsUnique();

        builder.HasOne(item => item.Cart)
            .WithMany(cart => cart.Items)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.ProductVariant)
            .WithMany()
            .HasForeignKey(item => item.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
