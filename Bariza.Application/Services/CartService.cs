using Bariza.Application.Common;
using Bariza.Application.DTOs.Orders;
using Bariza.Application.Interfaces.Repositories;
using Bariza.Application.Interfaces.Services;
using Bariza.Core.Entities;

namespace Bariza.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _carts;
    private readonly IProductRepository _products;

    public CartService(ICartRepository carts, IProductRepository products)
    {
        _carts = carts;
        _products = products;
    }

    public async Task<CartDto> GetCartAsync(int userId, CancellationToken cancellationToken = default)
    {
        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken)
            ?? await _carts.CreateAsync(userId, cancellationToken);

        return await MapCartAsync(cart, cancellationToken);
    }

    public async Task<CartDto> AddItemAsync(int userId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ValidationException("A cart item request is required.");
        }

        if (request.ProductId <= 0)
        {
            throw new ValidationException("ProductId must be greater than zero.");
        }

        if (request.Quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        var product = await _products.GetPublishedProductAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException("The requested product was not found.");
        }

        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken)
            ?? await _carts.CreateAsync(userId, cancellationToken);

        var item = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (item is null)
        {
            await _carts.AddItemAsync(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                CreatedUtc = DateTime.UtcNow,
            }, cancellationToken);
        }
        else
        {
            item.Quantity += request.Quantity;
            item.UpdatedUtc = DateTime.UtcNow;
            await _carts.SaveChangesAsync(cancellationToken);
        }

        cart.UpdatedUtc = DateTime.UtcNow;
        await _carts.SaveChangesAsync(cancellationToken);

        return await GetCartAsync(userId, cancellationToken);
    }

    public async Task<CartDto> UpdateItemAsync(int userId, int cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || request.Quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("The requested cart was not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null)
        {
            throw new NotFoundException("The requested cart item was not found.");
        }

        item.Quantity = request.Quantity;
        item.UpdatedUtc = DateTime.UtcNow;
        cart.UpdatedUtc = DateTime.UtcNow;

        await _carts.SaveChangesAsync(cancellationToken);

        return await GetCartAsync(userId, cancellationToken);
    }

    public async Task RemoveItemAsync(int userId, int cartItemId, CancellationToken cancellationToken = default)
    {
        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("The requested cart was not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is null)
        {
            throw new NotFoundException("The requested cart item was not found.");
        }

        await _carts.RemoveItemAsync(item, cancellationToken);
        cart.UpdatedUtc = DateTime.UtcNow;
        await _carts.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearCartAsync(int userId, CancellationToken cancellationToken = default)
    {
        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken);
        if (cart is null)
        {
            return;
        }

        foreach (var item in cart.Items.ToList())
        {
            await _carts.RemoveItemAsync(item, cancellationToken);
        }

        cart.Items.Clear();
        cart.UpdatedUtc = DateTime.UtcNow;
        await _carts.SaveChangesAsync(cancellationToken);
    }

    private async Task<CartDto> MapCartAsync(Cart cart, CancellationToken cancellationToken)
    {
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = new Dictionary<int, Product>();
        var stock = new Dictionary<int, int>();

        foreach (var id in productIds)
        {
            var product = await _products.GetPublishedProductAsync(id, cancellationToken);
            if (product is not null)
            {
                products[id] = product;
            }

            var items = await _products.GetStockAsync(id, cancellationToken);
            stock[id] = items.Sum(w => w.Qty);
        }

        var dtoItems = cart.Items
            .OrderBy(i => i.Id)
            .Select(i =>
            {
                products.TryGetValue(i.ProductId, out var product);
                var unitPrice = product?.DiscountPrice ?? product?.Price ?? 0m;

                return new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = product?.Name ?? string.Empty,
                    ImageUrl = product?.ImageUrl,
                    UnitPrice = unitPrice,
                    Quantity = i.Quantity,
                    Subtotal = unitPrice * i.Quantity,
                    SellerName = product?.SellerProfile?.StoreName,
                    IsAvailable = stock.TryGetValue(i.ProductId, out var qty) && qty >= i.Quantity,
                };
            })
            .ToList();

        return new CartDto
        {
            Id = cart.Id,
            Items = dtoItems,
            TotalAmount = dtoItems.Sum(i => i.Subtotal),
        };
    }
}
