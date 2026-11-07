using Bariza.Application.Services;
using Bariza.Core.Entities;
using Bariza.Infrastructure.Data;
using Bariza.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bariza.Tests;

public class DashboardTests
{
    [Fact]
    public async Task Dashboard_GroupsItemsCaseInsensitively_AndReportsWarehouseCounts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var country = new Country { Name = "Test Country", Code = "TC" };
        var city = new City { Name = "Test City", Country = country };
        var warehouseOne = new Warehouse
        {
            Name = "Warehouse One",
            Address = "One",
            City = city,
            Country = country,
        };
        var warehouseTwo = new Warehouse
        {
            Name = "Warehouse Two",
            Address = "Two",
            City = city,
            Country = country,
        };

        db.WarehouseItems.AddRange(
            new WarehouseItem
            {
                Warehouse = warehouseOne,
                ItemName = "Widget",
                SKUCode = "SKU-1",
                Qty = 5,
                CostPrice = 1m,
            },
            new WarehouseItem
            {
                Warehouse = warehouseTwo,
                ItemName = " widget ",
                SKUCode = null,
                Qty = 7,
                CostPrice = 1m,
            },
            new WarehouseItem
            {
                Warehouse = warehouseOne,
                ItemName = "Gadget",
                SKUCode = "SKU-2",
                Qty = 2,
                CostPrice = 1m,
            });

        await db.SaveChangesAsync();

        var repository = new DashboardRepository(db);
        var service = new DashboardService(repository);

        var dashboard = await service.GetAsync();

        Assert.Equal(2, dashboard.ItemCount);
        Assert.Equal(14, dashboard.TotalQuantity);

        var widget = dashboard.TopItems.Single(item =>
            string.Equals(item.ItemName, "Widget", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("widget", widget.ItemName, ignoreCase: true);
        Assert.Equal("SKU-1", widget.SkuCode);
        Assert.Equal(12, widget.TotalQuantity);
        Assert.Equal(2, widget.WarehouseCount);

        var gadget = dashboard.LowItems.Single(item => item.ItemName == "Gadget");
        Assert.Equal(1, gadget.WarehouseCount);

        var warehouse = dashboard.Warehouses.Single(item => item.Name == "Warehouse One");
        Assert.Equal(2, warehouse.ItemCount);
        Assert.Equal(7, warehouse.TotalQuantity);
    }
}
