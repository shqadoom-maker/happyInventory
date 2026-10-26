using HappyInventory.Application.DTOs.Cities;
using HappyInventory.Application.DTOs.Countries;
using HappyInventory.Application.DTOs.WarehouseItems;
using HappyInventory.Application.DTOs.Warehouses;
using HappyInventory.Application.Services;
using HappyInventory.Infrastructure.Data;
using HappyInventory.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HappyInventory.Tests;

public class ExternalCrudTests
{
    [Fact]
    public async Task CrudWorkflow_CreatesReadsUpdatesAndDeletesData()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var countryService = new CountryService(new CountryRepository(db));
        var cityService = new CityService(new CityRepository(db));
        var warehouseService = new WarehouseService(new WarehouseRepository(db));
        var itemService = new WarehouseItemService(new WarehouseItemRepository(db));

        var country = await countryService.CreateAsync(new CountryRequest
        {
            Name = "Testland",
            Code = "tl",
        });
        Assert.True(country.Id > 0);
        Assert.Equal("TL", country.Code);

        var city = await cityService.CreateAsync(new CityRequest
        {
            Name = "Test City",
            CountryId = country.Id,
        });
        Assert.Equal(country.Id, city.CountryId);

        var warehouse = await warehouseService.CreateAsync(new WarehouseRequest
        {
            Name = "Test Warehouse",
            Address = "1 Test Street",
            CityId = city.Id,
            CountryId = country.Id,
        });
        Assert.Equal(city.Id, warehouse.CityId);

        var item = await itemService.CreateAsync(new WarehouseItemRequest
        {
            ItemName = "Test Item",
            SKUCode = "EXT-1",
            Qty = 10,
            CostPrice = 2.5m,
            WarehouseId = warehouse.Id,
        });
        Assert.True(item.Id > 0);
        Assert.Equal("EXT-1", item.SKUCode);

        var fetchedItem = await itemService.GetByIdAsync(item.Id);
        Assert.Equal(10, fetchedItem.Qty);

        var pagedItems = await itemService.GetPagedAsync(warehouse.Id, 1, 10, "EXT-1");
        Assert.Equal(1, pagedItems.TotalCount);

        var updatedItem = await itemService.UpdateAsync(item.Id, new WarehouseItemRequest
        {
            ItemName = "Updated Item",
            SKUCode = "EXT-1",
            Qty = 7,
            CostPrice = 3m,
            WarehouseId = warehouse.Id,
        });
        Assert.Equal("Updated Item", updatedItem.ItemName);
        Assert.Equal(7, updatedItem.Qty);

        var updatedWarehouse = await warehouseService.UpdateAsync(warehouse.Id, new WarehouseRequest
        {
            Name = "Updated Warehouse",
            Address = "2 Test Street",
            CityId = city.Id,
            CountryId = country.Id,
        });
        Assert.Equal("Updated Warehouse", updatedWarehouse.Name);

        var updatedCity = await cityService.UpdateAsync(city.Id, new CityRequest
        {
            Name = "Updated City",
            CountryId = country.Id,
        });
        Assert.Equal("Updated City", updatedCity.Name);

        var updatedCountry = await countryService.UpdateAsync(country.Id, new CountryRequest
        {
            Name = "Updated Testland",
            Code = "tl",
        });
        Assert.Equal("Updated Testland", updatedCountry.Name);

        await itemService.DeleteAsync(item.Id);
        await warehouseService.DeleteAsync(warehouse.Id);
        await cityService.DeleteAsync(city.Id);
        await countryService.DeleteAsync(country.Id);

        Assert.Empty(await countryService.GetAllAsync());
        Assert.Empty(await cityService.GetAllAsync(null));
        Assert.Empty(await warehouseService.GetAllAsync());
    }
}
