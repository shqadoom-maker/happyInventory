using Bariza.Application.Common;
using Bariza.Application.DTOs.Orders;
using Bariza.Application.Interfaces.Repositories;
using Bariza.Application.Interfaces.Services;
using Bariza.Core.Entities;
using Bariza.Core.Enums;

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

        var product = request.ProductVariantId.HasValue
            ? (await _products.GetVariantAsync(request.ProductVariantId.Value, cancellationToken))?.Product
            : await _products.GetPublishedProductAsync(request.ProductId, cancellationToken);

        if (product is null || !product.IsPublished || !product.IsActive || product.ApprovalStatus != ProductApprovalStatus.Approved)
        {
            throw new NotFoundException("The requested product was not found.");
        }

        ProductVariant? variant = null;
        if (request.ProductVariantId.HasValue)
        {
            variant = await _products.GetVariantAsync(request.ProductVariantId.Value, cancellationToken);
            if (variant is null || variant.ProductId != product.Id)
            {
                throw new NotFoundException("The requested variant was not found.");
            }

            if (variant.Stock < request.Quantity)
            {
                throw new ConflictException($"Insufficient stock for the selected variant. Available quantity is {variant.Stock}.");
            }
        }
        else
        {
            var stock = await _products.GetStockAsync(product.Id, cancellationToken);
            var available = stock.Sum(w => w.Qty);
            if (available < request.Quantity)
            {
                throw new ConflictException($"Insufficient stock for '{product.Name}'. Available quantity is {available}.");
            }
        }

        var cart = await _carts.GetByUserIdAsync(userId, cancellationToken)
            ?? await _carts.CreateAsync(userId, cancellationToken);

        var item = cart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId &&
            i.ProductVariantId == request.ProductVariantId);

        if (item is null)
        {
            await _carts.AddItemAsync(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
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

        await _carts.ClearAsync(cart.Id, cancellationToken);
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

        var dtoItems = new List<CartItemDto>();

        foreach (var i in cart.Items.OrderBy(i => i.Id))
        {
            products.TryGetValue(i.ProductId, out var product);
            ProductVariant? variant = null;

            if (i.ProductVariantId.HasValue)
            {
                variant = await _products.GetVariantAsync(i.ProductVariantId.Value, cancellationToken);
            }

            var unitPrice = variant?.PriceOverride ?? product?.DiscountPrice ?? product?.Price ?? 0m;

            dtoItems.Add(new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductVariantId = i.ProductVariantId,
                ProductName = product?.Name ?? string.Empty,
                VariantName = variant?.Name,
                VariantValue = variant?.Value,
                ImageUrl = product?.ImageUrl,
                UnitPrice = unitPrice,
                Quantity = i.Quantity,
                Subtotal = unitPrice * i.Quantity,
                SellerName = product?.SellerProfile?.StoreName,
                IsAvailable = stock.TryGetValue(i.ProductId, out var qty) && qty >= i.Quantity,
            });
        }

        return new CartDto
        {
            Id = cart.Id,
            Items = dtoItems,
            TotalAmount = dtoItems.Sum(i => i.Subtotal),
        };
    }
}
