using Bariza.Application.Interfaces.Repositories;
using Bariza.Core.Entities;
using Bariza.Core.Enums;
using Bariza.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bariza.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _db;

    public CartRepository(AppDbContext db) => _db = db;

    public Task<Cart?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public async Task<Cart> CreateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var cart = new Cart { UserId = userId, CreatedUtc = DateTime.UtcNow };
        await _db.Carts.AddAsync(cart, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return cart;
    }

    public Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default) =>
        _db.CartItems.AddAsync(item, cancellationToken).AsTask();

    public void RemoveItem(CartItem item) => _db.CartItems.Remove(item);

    public Task RemoveItemAsync(CartItem item, CancellationToken cancellationToken = default)
    {
        RemoveItem(item);
        return Task.CompletedTask;
    }

    public async Task ClearAsync(int cartId, CancellationToken cancellationToken = default)
    {
        var items = await _db.CartItems.Where(i => i.CartId == cartId).ToListAsync(cancellationToken);
        _db.CartItems.RemoveRange(items);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db) => _db = db;

    public Task<Product?> GetPublishedProductAsync(int productId, CancellationToken cancellationToken = default) =>
        _db.Products
            .Include(p => p.SellerProfile)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsPublished && p.IsActive && p.ApprovalStatus == ProductApprovalStatus.Approved, cancellationToken);

    public Task<ProductVariant?> GetVariantAsync(int variantId, CancellationToken cancellationToken = default) =>
        _db.ProductVariants
            .Include(v => v.Product).ThenInclude(p => p.SellerProfile)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.IsActive, cancellationToken);

    public async Task<IReadOnlyList<WarehouseItem>> GetStockAsync(int productId, CancellationToken cancellationToken = default) =>
        await _db.WarehouseItems
            .Include(w => w.Warehouse)
            .Where(w => w.ProductId == productId)
            .OrderByDescending(w => w.Qty)
            .ToListAsync(cancellationToken);
}

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db) => _db = db;

    public Task AddOrderAsync(Order order, CancellationToken cancellationToken = default) =>
        _db.Orders.AddAsync(order, cancellationToken).AsTask();

    public async Task<Order?> GetCustomerOrderAsync(int userId, int orderId, CancellationToken cancellationToken = default) =>
        await _db.Orders
            .AsNoTracking()
            .Include(o => o.ShippingAddress)
            .Include(o => o.SellerOrders).ThenInclude(s => s.SellerProfile)
            .Include(o => o.SellerOrders).ThenInclude(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(int userId, CancellationToken cancellationToken = default) =>
        await _db.Orders
            .AsNoTracking()
            .Include(o => o.SellerOrders)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedUtc)
            .ToListAsync(cancellationToken);

    public Task<Order?> GetOrderByCheckoutReferenceAsync(int userId, string checkoutReference, CancellationToken cancellationToken = default) =>
        _db.Orders
            .AsNoTracking()
            .Include(o => o.ShippingAddress)
            .Include(o => o.SellerOrders).ThenInclude(s => s.SellerProfile)
            .Include(o => o.SellerOrders).ThenInclude(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.CheckoutReference == checkoutReference, cancellationToken);

    public async Task<SellerOrder?> GetSellerOrderAsync(int userId, int sellerOrderId, CancellationToken cancellationToken = default)
    {
        var sellerProfile = await _db.SellerProfiles.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (sellerProfile is null) return null;

        return await _db.SellerOrders
            .AsNoTracking()
            .Include(s => s.Order).ThenInclude(o => o.ShippingAddress)
            .Include(s => s.Order).ThenInclude(o => o.User)
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == sellerOrderId && s.SellerProfileId == sellerProfile.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<SellerOrder>> GetSellerOrdersAsync(int userId, CancellationToken cancellationToken = default)
    {
        var sellerProfile = await _db.SellerProfiles.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (sellerProfile is null) return Array.Empty<SellerOrder>();

        return await _db.SellerOrders
            .AsNoTracking()
            .Include(s => s.Order).ThenInclude(o => o.User)
            .Include(s => s.Items)
            .Where(s => s.SellerProfileId == sellerProfile.Id)
            .OrderByDescending(s => s.CreatedUtc)
            .ToListAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<Order>> GetAdminOrdersAsync(CancellationToken cancellationToken = default) =>
        await _db.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.SellerOrders)
            .OrderByDescending(o => o.CreatedUtc)
            .ToListAsync(cancellationToken);

    public async Task<Order?> GetAdminOrderAsync(int orderId, CancellationToken cancellationToken = default) =>
        await _db.Orders
            .AsNoTracking()
            .Include(o => o.ShippingAddress)
            .Include(o => o.User)
            .Include(o => o.SellerOrders).ThenInclude(s => s.SellerProfile)
            .Include(o => o.SellerOrders).ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    public async Task<SellerOrder?> GetAdminSellerOrderAsync(int sellerOrderId, CancellationToken cancellationToken = default) =>
        await _db.SellerOrders
            .AsNoTracking()
            .Include(s => s.Order).ThenInclude(o => o.ShippingAddress)
            .Include(s => s.Order).ThenInclude(o => o.User)
            .Include(s => s.SellerProfile)
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sellerOrderId, cancellationToken);

    public async Task<IReadOnlyList<SellerOrder>> GetAdminSellerOrdersAsync(CancellationToken cancellationToken = default) =>
        await _db.SellerOrders
            .AsNoTracking()
            .Include(s => s.Order).ThenInclude(o => o.User)
            .Include(s => s.SellerProfile)
            .Include(s => s.Items)
            .OrderByDescending(s => s.CreatedUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShippingAddress>> GetAddressesAsync(int userId, CancellationToken cancellationToken = default) =>
        await _db.ShippingAddresses.Where(a => a.Orders.Any(o => o.UserId == userId)).ToListAsync(cancellationToken);

    public async Task<ShippingAddress?> GetAddressAsync(int userId, int addressId, CancellationToken cancellationToken = default) =>
        await _db.ShippingAddresses.FirstOrDefaultAsync(a => a.Id == addressId && a.Orders.Any(o => o.UserId == userId), cancellationToken);

    public Task AddAddressAsync(ShippingAddress address, CancellationToken cancellationToken = default) =>
        _db.ShippingAddresses.AddAsync(address, cancellationToken).AsTask();

    public Task UpdateAddressAsync(ShippingAddress address, CancellationToken cancellationToken = default)
    {
        _db.ShippingAddresses.Update(address);
        return Task.CompletedTask;
    }

    public Task DeleteAddressAsync(ShippingAddress address, CancellationToken cancellationToken = default)
    {
        _db.ShippingAddresses.Remove(address);
        return Task.CompletedTask;
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
