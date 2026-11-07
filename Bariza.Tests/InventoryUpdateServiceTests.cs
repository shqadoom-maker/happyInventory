using Bariza.Application.Common;
using Bariza.Application.DTOs.Integrations;
using Bariza.Application.Interfaces.Repositories;
using Bariza.Application.Services;
using Bariza.Core.Entities;
using Xunit;

namespace Bariza.Tests;

public class InventoryUpdateServiceTests
{
    [Fact]
    public async Task ApplyAsync_DeductsQuantity_WhenStockIsAvailable()
    {
        var item = new WarehouseItem
        {
            Id = 10,
            WarehouseId = 2,
            SKUCode = "SKU-1",
            ItemName = "Widget",
            Qty = 12,
        };

        var repository = new FakeInventoryUpdateRepository(item);
        var service = new InventoryUpdateService(repository);

        var response = await service.ApplyAsync(new InventoryUpdateRequest
        {
            WarehouseId = 2,
            SKUCode = "sku-1",
            Quantity = 5,
        });

        Assert.Equal(12, response.PreviousQuantity);
        Assert.Equal(7, response.RemainingQuantity);
        Assert.Equal(7, item.Qty);
        Assert.True(repository.Saved);
    }

    [Fact]
    public async Task ApplyAsync_ThrowsConflict_WhenStockIsInsufficient()
    {
        var item = new WarehouseItem
        {
            Id = 10,
            WarehouseId = 2,
            SKUCode = "SKU-1",
            ItemName = "Widget",
            Qty = 3,
        };

        var repository = new FakeInventoryUpdateRepository(item);
        var service = new InventoryUpdateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.ApplyAsync(new InventoryUpdateRequest
        {
            WarehouseId = 2,
            SKUCode = "SKU-1",
            Quantity = 4,
        }));

        Assert.Equal(3, item.Qty);
        Assert.False(repository.Saved);
    }

    [Fact]
    public async Task ApplyAsync_ThrowsNotFound_WhenSkuDoesNotExist()
    {
        var repository = new FakeInventoryUpdateRepository();
        var service = new InventoryUpdateService(repository);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ApplyAsync(new InventoryUpdateRequest
        {
            WarehouseId = 2,
            SKUCode = "missing",
            Quantity = 1,
        }));
    }

    [Fact]
    public async Task ApplyAsync_ThrowsInvalidOperation_WhenQuantityIsNotPositive()
    {
        var repository = new FakeInventoryUpdateRepository();
        var service = new InventoryUpdateService(repository);

        await Assert.ThrowsAsync<ValidationException>(() => service.ApplyAsync(new InventoryUpdateRequest
        {
            WarehouseId = 2,
            SKUCode = "SKU-1",
            Quantity = 0,
        }));
    }

    [Fact]
    public async Task ApplyAsync_ThrowsConflict_WhenSkuIsAmbiguous()
    {
        var repository = new FakeInventoryUpdateRepository(
            new WarehouseItem { Id = 1, WarehouseId = 2, SKUCode = "SKU-1", ItemName = "A", Qty = 5 },
            new WarehouseItem { Id = 2, WarehouseId = 2, SKUCode = "SKU-1", ItemName = "B", Qty = 5 });

        var service = new InventoryUpdateService(repository);

        await Assert.ThrowsAsync<ConflictException>(() => service.ApplyAsync(new InventoryUpdateRequest
        {
            WarehouseId = 2,
            SKUCode = "SKU-1",
            Quantity = 1,
        }));
    }

    [Fact]
    public async Task ApplyAsync_ReturnsStoredResponse_WhenIdempotencyKeyIsRetried()
    {
        var item = new WarehouseItem
        {
            Id = 10,
            WarehouseId = 2,
            SKUCode = "SKU-1",
            ItemName = "Widget",
            Qty = 12,
        };

        var repository = new FakeInventoryUpdateRepository(item);
        var service = new InventoryUpdateService(repository);

        var request = new InventoryUpdateRequest
        {
            WarehouseId = 2,
            SKUCode = "sku-1",
            Quantity = 5,
        };

        var first = await service.ApplyAsync(request, "order-123");
        var replay = await service.ApplyAsync(request, "order-123");

        Assert.Equal(first.WarehouseItemId, replay.WarehouseItemId);
        Assert.Equal(first.PreviousQuantity, replay.PreviousQuantity);
        Assert.Equal(first.RemainingQuantity, replay.RemainingQuantity);
        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(7, item.Qty);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task ApplyAsync_ThrowsConflict_WhenIdempotencyKeyIsReusedWithDifferentPayload()
    {
        var item = new WarehouseItem
        {
            Id = 10,
            WarehouseId = 2,
            SKUCode = "SKU-1",
            ItemName = "Widget",
            Qty = 12,
        };

        var repository = new FakeInventoryUpdateRepository(item);
        var service = new InventoryUpdateService(repository);

        await service.ApplyAsync(new InventoryUpdateRequest
        {
            WarehouseId = 2,
            SKUCode = "SKU-1",
            Quantity = 2,
        }, "order-456");

        await Assert.ThrowsAsync<ConflictException>(() => service.ApplyAsync(new InventoryUpdateRequest
        {
            WarehouseId = 2,
            SKUCode = "SKU-1",
            Quantity = 3,
        }, "order-456"));

        Assert.Equal(10, item.Qty);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task ApplyAsync_ThrowsInvalidOperation_WhenIdempotencyKeyIsTooLong()
    {
        var repository = new FakeInventoryUpdateRepository(
            new WarehouseItem { Id = 10, WarehouseId = 2, SKUCode = "SKU-1", ItemName = "Widget", Qty = 12 });

        var service = new InventoryUpdateService(repository);

        await Assert.ThrowsAsync<ValidationException>(() => service.ApplyAsync(new InventoryUpdateRequest
        {
            WarehouseId = 2,
            SKUCode = "SKU-1",
            Quantity = 1,
        }, new string('x', 101)));
    }

    private sealed class FakeInventoryUpdateRepository : IInventoryUpdateRepository
    {
        private readonly List<WarehouseItem> _items;
        private readonly Dictionary<string, InventoryUpdateRecord> _records = new(StringComparer.Ordinal);

        public FakeInventoryUpdateRepository(params WarehouseItem[] items)
        {
            _items = items.ToList();
        }

        public int SaveCount { get; private set; }

        public bool Saved => SaveCount > 0;

        public Task<InventoryUpdateRecord?> GetIdempotencyRecordAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            _records.TryGetValue(idempotencyKey, out var record);
            return Task.FromResult(record);
        }

        public Task<IReadOnlyList<WarehouseItem>> FindBySkuAsync(
            int warehouseId,
            string skuCode,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<WarehouseItem> matches = _items
                .Where(item =>
                    item.WarehouseId == warehouseId &&
                    string.Equals(item.SKUCode, skuCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(matches);
        }

        public Task<(IReadOnlyList<InventoryUpdateRecordDto> Items, int TotalCount)> GetPagedAsync(
            int? warehouseId,
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<InventoryUpdateRecordDto> empty = Array.Empty<InventoryUpdateRecordDto>();
            return Task.FromResult((empty, 0));
        }

        public void AddIdempotencyRecord(InventoryUpdateRecord record)
        {
            _records[record.IdempotencyKey] = record;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
