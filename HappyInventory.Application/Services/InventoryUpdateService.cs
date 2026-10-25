using HappyInventory.Application.Common;
using HappyInventory.Application.DTOs.Integrations;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Services;

/// <summary>
/// Deducts stock for an inbound eCommerce inventory update. The update is
/// applied to the existing warehouse-item record selected by warehouse and SKU.
/// When an idempotency key is supplied, the result is stored in the same
/// transaction as the stock change so a retry cannot deduct twice.
/// </summary>
public class InventoryUpdateService : IInventoryUpdateService
{
    private const int MaxIdempotencyKeyLength = 100;

    private readonly IInventoryUpdateRepository _updates;

    public InventoryUpdateService(IInventoryUpdateRepository updates)
    {
        _updates = updates;
    }

    public async Task<InventoryUpdateResponse> ApplyAsync(
        InventoryUpdateRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new InvalidOperationException("An inventory update request is required.");
        }

        if (request.WarehouseId <= 0)
        {
            throw new InvalidOperationException("WarehouseId must be greater than zero.");
        }

        var sku = request.SKUCode?.Trim();

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new InvalidOperationException("SKUCode is required.");
        }

        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        var key = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim();

        if (key is { Length: > MaxIdempotencyKeyLength })
        {
            throw new InvalidOperationException(
                $"Idempotency-Key must be {MaxIdempotencyKeyLength} characters or fewer.");
        }

        if (key is not null)
        {
            var existing = await _updates.GetIdempotencyRecordAsync(key, cancellationToken);

            if (existing is not null)
            {
                if (existing.WarehouseId != request.WarehouseId ||
                    !string.Equals(existing.SKUCode, sku, StringComparison.OrdinalIgnoreCase) ||
                    existing.Quantity != request.Quantity)
                {
                    throw new ConflictException(
                        "The Idempotency-Key has already been used with a different inventory update request.");
                }

                return ToResponse(existing);
            }
        }

        var matches = await _updates.FindBySkuAsync(
            request.WarehouseId,
            sku,
            cancellationToken);

        if (matches.Count == 0)
        {
            throw new KeyNotFoundException("The requested warehouse item was not found.");
        }

        if (matches.Count > 1)
        {
            throw new ConflictException(
                "Multiple warehouse items match the supplied SKU. The inventory update cannot be applied unambiguously.");
        }

        var item = matches[0];

        if (item.Qty < request.Quantity)
        {
            throw new ConflictException(
                $"Insufficient stock for SKU '{sku}'. Available quantity is {item.Qty}, requested deduction is {request.Quantity}.");
        }

        var previousQuantity = item.Qty;
        item.Qty -= request.Quantity;

        var response = new InventoryUpdateResponse
        {
            WarehouseItemId = item.Id,
            WarehouseId = item.WarehouseId,
            SKUCode = item.SKUCode ?? sku,
            ItemName = item.ItemName,
            PreviousQuantity = previousQuantity,
            RemainingQuantity = item.Qty,
        };

        if (key is not null)
        {
            _updates.AddIdempotencyRecord(new InventoryUpdateRecord
            {
                IdempotencyKey = key,
                WarehouseId = response.WarehouseId,
                SKUCode = response.SKUCode,
                Quantity = request.Quantity,
                WarehouseItemId = response.WarehouseItemId,
                ItemName = response.ItemName,
                PreviousQuantity = response.PreviousQuantity,
                RemainingQuantity = response.RemainingQuantity,
                CreatedUtc = DateTime.UtcNow,
            });
        }

        await _updates.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static InventoryUpdateResponse ToResponse(InventoryUpdateRecord record)
    {
        return new InventoryUpdateResponse
        {
            WarehouseItemId = record.WarehouseItemId,
            WarehouseId = record.WarehouseId,
            SKUCode = record.SKUCode,
            ItemName = record.ItemName,
            PreviousQuantity = record.PreviousQuantity,
            RemainingQuantity = record.RemainingQuantity,
        };
    }
}
