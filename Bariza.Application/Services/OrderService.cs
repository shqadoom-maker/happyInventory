using Bariza.Application.Common;
using Bariza.Application.DTOs.Orders;
using Bariza.Application.Interfaces.Repositories;
using Bariza.Application.Interfaces.Services;
using Bariza.Core.Entities;
using Bariza.Core.Enums;

namespace Bariza.Application.Services;

public class OrderService : IOrderService
{
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;
    private readonly IWarehouseItemRepository _warehouseItems;
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        ICartRepository carts,
        IProductRepository products,
        IWarehouseItemRepository warehouseItems,
        IOrderRepository orders,
        IUnitOfWork unitOfWork)
    {
        _carts = carts;
        _products = products;
        _warehouseItems = warehouseItems;
        _orders = orders;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> CheckoutAsync(
        int userId,
        ShippingAddressRequest shipping,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (shipping is null)
        {
            throw new ValidationException("Shipping information is required.");
        }

        ValidateShipping(shipping);

        var checkoutReference = string.IsNullOrWhiteSpace(idempotencyKey)
            ? Guid.NewGuid().ToString("N")
            : idempotencyKey.Trim();

        var existing = await _orders.GetOrderByCheckoutReferenceAsync(userId, checkoutReference, cancellationToken);
        if (existing is not null)
        {
            return MapOrder(existing);
        }

        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            throw new ValidationException("The cart is empty.");
        }

        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            var shippingAddress = new ShippingAddress
            {
                FullName = shipping.FullName.Trim(),
                Phone = shipping.Phone.Trim(),
                Country = shipping.Country.Trim(),
                City = shipping.City.Trim(),
                Address = shipping.Address.Trim(),
            };

            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                CheckoutReference = checkoutReference,
                UserId = userId,
                ShippingAddress = shippingAddress,
                Status = OrderStatus.Pending,
                CreatedUtc = DateTime.UtcNow,
            };

            var cartItems = cart.Items.ToList();
            var grouped = cartItems
                .GroupBy(i => i.ProductId)
                .ToList();

            // Validate and prepare items.
            var preparedItems = new List<(CartItem CartItem, Product Product, WarehouseItem WarehouseItem, decimal Price, int Quantity)>();

            foreach (var cartItem in cartItems)
            {
                if (cartItem.Quantity <= 0)
                {
                    throw new ValidationException("Quantity must be greater than zero.");
                }

                var product = await _products.GetPublishedProductAsync(cartItem.ProductId, cancellationToken);
                if (product is null)
                {
                    throw new ValidationException($"Product {cartItem.ProductId} is not available.");
                }

                var stock = await _products.GetStockAsync(cartItem.ProductId, cancellationToken);
                var warehouseItem = stock.FirstOrDefault(w => w.Qty >= cartItem.Quantity);

                if (warehouseItem is null)
                {
                    var available = stock.Sum(w => w.Qty);
                    throw new ConflictException(
                        $"Insufficient stock for '{product.Name}'. Available quantity is {available}, requested quantity is {cartItem.Quantity}.");
                }

                var price = product.DiscountPrice ?? product.Price;
                preparedItems.Add((cartItem, product, warehouseItem, price, cartItem.Quantity));
            }

            var sellerGroups = preparedItems.GroupBy(p => p.Product.SellerProfileId);

            foreach (var group in sellerGroups)
            {
                var sellerOrder = new SellerOrder
                {
                    SellerProfileId = group.Key ?? 0,
                    Status = OrderStatus.Pending,
                    CreatedUtc = DateTime.UtcNow,
                };

                if (sellerOrder.SellerProfileId <= 0)
                {
                    throw new ValidationException("A product in the cart does not have a seller.");
                }

                foreach (var prepared in group)
                {
                    var subtotal = prepared.Price * prepared.Quantity;
                    sellerOrder.Items.Add(new OrderItem
                    {
                        ProductId = prepared.Product.Id,
                        WarehouseItemId = prepared.WarehouseItem.Id,
                        ProductName = prepared.Product.Name,
                        UnitPrice = prepared.Price,
                        Quantity = prepared.Quantity,
                        Subtotal = subtotal,
                    });

                    sellerOrder.TotalAmount += subtotal;

                    // Deduct stock.
                    prepared.WarehouseItem.Qty -= prepared.Quantity;
                }

                order.SellerOrders.Add(sellerOrder);
                order.TotalAmount += sellerOrder.TotalAmount;
            }

            await _orders.AddOrderAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Clear cart.
            await _carts.ClearAsync(cart.Id, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);

            var created = await _orders.GetOrderByCheckoutReferenceAsync(userId, checkoutReference, cancellationToken);
            return MapOrder(created ?? order);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _unitOfWork.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(int userId, CancellationToken cancellationToken = default)
    {
        var orders = await _orders.GetCustomerOrdersAsync(userId, cancellationToken);
        var result = new List<OrderDto>();

        foreach (var order in orders)
        {
            var full = await _orders.GetCustomerOrderAsync(userId, order.Id, cancellationToken);
            if (full is not null)
            {
                result.Add(MapOrder(full));
            }
        }

        return result;
    }

    public async Task<OrderDto> GetOrderAsync(int userId, int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetCustomerOrderAsync(userId, orderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException("The requested order was not found.");
        }

        return MapOrder(order);
    }

    public async Task CancelOrderAsync(int userId, int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetCustomerOrderAsync(userId, orderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException("The requested order was not found.");
        }

        if (order.StockRestored)
        {
            throw new ConflictException("The order has already been cancelled or rejected.");
        }

        if (order.SellerOrders.Any(s => s.Status is OrderStatus.Shipped or OrderStatus.Delivered))
        {
            throw new ConflictException("The order cannot be cancelled after it has shipped.");
        }

        if (order.SellerOrders.Any(s => s.Status is OrderStatus.Cancelled or OrderStatus.Rejected))
        {
            throw new ConflictException("The order has already been cancelled or rejected.");
        }

        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            foreach (var sellerOrder in order.SellerOrders)
            {
                sellerOrder.Status = OrderStatus.Cancelled;
                sellerOrder.UpdatedUtc = DateTime.UtcNow;
            }

            order.Status = OrderStatus.Cancelled;
            order.StockRestored = true;
            order.UpdatedUtc = DateTime.UtcNow;

            foreach (var sellerOrder in order.SellerOrders)
            {
                foreach (var item in sellerOrder.Items)
                {
                    if (item.WarehouseItemId.HasValue)
                    {
                        var warehouseItem = await _warehouseItems.GetForUpdateAsync(item.WarehouseItemId.Value, cancellationToken);
                        if (warehouseItem is not null)
                        {
                            warehouseItem.Qty += item.Quantity;
                        }
                    }
                }
            }

            await _orders.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _unitOfWork.DisposeAsync();
        }
    }

    public async Task<IReadOnlyList<SellerOrderDto>> GetSellerOrdersAsync(int userId, CancellationToken cancellationToken = default)
    {
        var sellerOrders = await _orders.GetSellerOrdersAsync(userId, cancellationToken);
        return sellerOrders.Select(MapSellerOrder).ToList();
    }

    public async Task<SellerOrderDto> GetSellerOrderAsync(int userId, int sellerOrderId, CancellationToken cancellationToken = default)
    {
        var sellerOrder = await _orders.GetSellerOrderAsync(userId, sellerOrderId, cancellationToken);
        if (sellerOrder is null)
        {
            throw new NotFoundException("The requested seller order was not found.");
        }

        return MapSellerOrder(sellerOrder);
    }

    public async Task UpdateSellerOrderStatusAsync(
        int userId,
        int sellerOrderId,
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        var sellerOrder = await _orders.GetSellerOrderAsync(userId, sellerOrderId, cancellationToken);
        if (sellerOrder is null)
        {
            throw new NotFoundException("The requested seller order was not found.");
        }

        if (!IsValidTransition(sellerOrder.Status, status))
        {
            throw new ValidationException($"Invalid status transition from {sellerOrder.Status} to {status}.");
        }

        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            sellerOrder.Status = status;
            sellerOrder.UpdatedUtc = DateTime.UtcNow;

            if (status == OrderStatus.Rejected && !sellerOrder.StockRestored)
            {
                foreach (var item in sellerOrder.Items)
                {
                    if (item.WarehouseItemId.HasValue)
                    {
                        var warehouseItem = await _warehouseItems.GetForUpdateAsync(item.WarehouseItemId.Value, cancellationToken);
                        if (warehouseItem is not null)
                        {
                            warehouseItem.Qty += item.Quantity;
                        }
                    }
                }

                sellerOrder.StockRestored = true;
            }

            // Update parent order status if all seller orders are terminal.
            var parent = sellerOrder.Order;
            if (parent is not null && parent.SellerOrders.All(s => s.Status is OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Rejected))
            {
                if (parent.SellerOrders.Any(s => s.Status == OrderStatus.Cancelled))
                {
                    parent.Status = OrderStatus.Cancelled;
                }
                else if (parent.SellerOrders.Any(s => s.Status == OrderStatus.Rejected))
                {
                    parent.Status = OrderStatus.Rejected;
                }
                else
                {
                    parent.Status = OrderStatus.Delivered;
                }

                parent.UpdatedUtc = DateTime.UtcNow;
            }

            await _orders.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _unitOfWork.DisposeAsync();
        }
    }

    private static void ValidateShipping(ShippingAddressRequest shipping)
    {
        if (string.IsNullOrWhiteSpace(shipping.FullName))
        {
            throw new ValidationException("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(shipping.Phone))
        {
            throw new ValidationException("Phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(shipping.Country))
        {
            throw new ValidationException("Country is required.");
        }

        if (string.IsNullOrWhiteSpace(shipping.City))
        {
            throw new ValidationException("City is required.");
        }

        if (string.IsNullOrWhiteSpace(shipping.Address))
        {
            throw new ValidationException("Address is required.");
        }
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    private static bool IsValidTransition(OrderStatus current, OrderStatus next)
    {
        if (current == next)
        {
            return false;
        }

        return current switch
        {
            OrderStatus.Pending => next is OrderStatus.Confirmed or OrderStatus.Rejected or OrderStatus.Cancelled,
            OrderStatus.Confirmed => next is OrderStatus.Processing or OrderStatus.Cancelled or OrderStatus.Rejected,
            OrderStatus.Processing => next is OrderStatus.Shipped or OrderStatus.Cancelled,
            OrderStatus.Shipped => next is OrderStatus.Delivered,
            _ => false,
        };
    }

    private static OrderDto MapOrder(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CheckoutReference = order.CheckoutReference,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedUtc = order.CreatedUtc,
            ShippingAddress = new ShippingAddressDto
            {
                Id = order.ShippingAddress?.Id ?? 0,
                FullName = order.ShippingAddress?.FullName ?? string.Empty,
                Phone = order.ShippingAddress?.Phone ?? string.Empty,
                Country = order.ShippingAddress?.Country ?? string.Empty,
                City = order.ShippingAddress?.City ?? string.Empty,
                Address = order.ShippingAddress?.Address ?? string.Empty,
            },
            SellerOrders = order.SellerOrders
                .OrderBy(s => s.Id)
                .Select(MapSellerOrder)
                .ToList(),
        };
    }

    private static SellerOrderDto MapSellerOrder(SellerOrder sellerOrder)
    {
        return new SellerOrderDto
        {
            Id = sellerOrder.Id,
            OrderId = sellerOrder.OrderId,
            OrderNumber = sellerOrder.Order?.OrderNumber ?? string.Empty,
            SellerStoreName = sellerOrder.SellerProfile?.StoreName ?? string.Empty,
            Status = sellerOrder.Status,
            TotalAmount = sellerOrder.TotalAmount,
            CreatedUtc = sellerOrder.CreatedUtc,
            ShippingAddress = new ShippingAddressDto
            {
                Id = sellerOrder.Order?.ShippingAddress?.Id ?? 0,
                FullName = sellerOrder.Order?.ShippingAddress?.FullName ?? string.Empty,
                Phone = sellerOrder.Order?.ShippingAddress?.Phone ?? string.Empty,
                Country = sellerOrder.Order?.ShippingAddress?.Country ?? string.Empty,
                City = sellerOrder.Order?.ShippingAddress?.City ?? string.Empty,
                Address = sellerOrder.Order?.ShippingAddress?.Address ?? string.Empty,
            },
            CustomerName = sellerOrder.Order?.User?.FullName ?? sellerOrder.Order?.User?.Username ?? string.Empty,
            Items = sellerOrder.Items
                .OrderBy(i => i.Id)
                .Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ImageUrl = i.Product?.ImageUrl,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Subtotal = i.Subtotal,
                    SellerName = sellerOrder.SellerProfile?.StoreName,
                })
                .ToList(),
        };
    }
}
