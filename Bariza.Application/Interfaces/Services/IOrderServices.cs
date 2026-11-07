using Bariza.Application.DTOs.Orders;
using Bariza.Core.Entities;

namespace Bariza.Application.Interfaces.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(int userId, CancellationToken cancellationToken = default);
    Task<CartDto> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateItemAsync(int userId, int cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(int userId, int cartItemId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(int userId, CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<OrderDto> CheckoutAsync(int userId, ShippingAddressRequest shipping, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(int userId, CancellationToken cancellationToken = default);
    Task<OrderDto> GetOrderAsync(int userId, int orderId, CancellationToken cancellationToken = default);
    Task CancelOrderAsync(int userId, int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SellerOrderDto>> GetSellerOrdersAsync(int userId, CancellationToken cancellationToken = default);
    Task<SellerOrderDto> GetSellerOrderAsync(int userId, int sellerOrderId, CancellationToken cancellationToken = default);
    Task UpdateSellerOrderStatusAsync(int userId, int sellerOrderId, OrderStatus status, CancellationToken cancellationToken = default);
}
