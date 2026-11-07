using Bariza.Core.Entities;

namespace Bariza.Application.Interfaces.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Cart> CreateAsync(int userId, CancellationToken cancellationToken = default);
    Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(CartItem item, CancellationToken cancellationToken = default);
    Task ClearAsync(int cartId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IProductRepository
{
    Task<Product?> GetPublishedProductAsync(int productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseItem>> GetStockAsync(int productId, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetCustomerOrderAsync(int userId, int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetCustomerOrdersAsync(int userId, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderByCheckoutReferenceAsync(int userId, string checkoutReference, CancellationToken cancellationToken = default);
    Task<SellerOrder?> GetSellerOrderAsync(int userId, int sellerOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SellerOrder>> GetSellerOrdersAsync(int userId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
