using System.Net;
using System.Net.Http.Json;
using Bariza.Application.Common;
using Bariza.Application.DTOs.Marketplace;
using Bariza.Core.Entities;
using Bariza.Core.Enums;
using Bariza.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bariza.Tests;

public class MarketplaceCatalogTests : IClassFixture<HappyInventoryApiFactory>, IAsyncLifetime
{
    private readonly HappyInventoryApiFactory _factory;
    private readonly HttpClient _client;

    public MarketplaceCatalogTests(HappyInventoryApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await SeedCatalogAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProductsEndpoint_IsAnonymous_AndReturnsOnlyPublicProducts()
    {
        var response = await _client.GetAsync("/api/marketplace/products?pageSize=12");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResponse<MarketplaceProductDto>>();

        Assert.NotNull(page);
        Assert.Equal(4, page!.TotalCount);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(12, page.PageSize);
        Assert.Equal(1, page.TotalPages);
        Assert.Equal(
            new[] { "Desk Delta", "Chair Gamma", "Phone Beta", "Laptop Alpha" },
            page.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Search_MatchesNameDescriptionAndSku_CaseInsensitively()
    {
        var byName = await GetProductsAsync("?search=laptop");
        var byDescription = await GetProductsAsync("?search=ERGONOMIC");
        var bySku = await GetProductsAsync("?search=desk-1");
        var emptySearch = await GetProductsAsync("?search=%20%20");

        Assert.Single(byName.Items);
        Assert.Equal("Laptop Alpha", byName.Items[0].Name);

        Assert.Single(byDescription.Items);
        Assert.Equal("Chair Gamma", byDescription.Items[0].Name);

        Assert.Single(bySku.Items);
        Assert.Equal("Desk Delta", bySku.Items[0].Name);

        Assert.Equal(4, emptySearch.TotalCount);
    }

    [Fact]
    public async Task CategoryFilter_ReturnsOnlySelectedCategory()
    {
        var electronics = await GetProductsAsync("?categoryId=1");
        var home = await GetProductsAsync("?categoryId=2");

        Assert.Equal(2, electronics.TotalCount);
        Assert.All(electronics.Items, item => Assert.Equal(1, item.CategoryId));

        Assert.Equal(2, home.TotalCount);
        Assert.All(home.Items, item => Assert.Equal(2, item.CategoryId));
    }

    [Fact]
    public async Task PriceFilter_UsesEffectiveDiscountPrice()
    {
        var cheap = await GetProductsAsync("?maxPrice=160");

        Assert.Single(cheap.Items);
        Assert.Equal("Chair Gamma", cheap.Items[0].Name);
        Assert.Equal(150m, cheap.Items[0].DiscountPrice);

        var range = await GetProductsAsync("?minPrice=300&maxPrice=600");

        Assert.Equal(2, range.TotalCount);
        Assert.Contains(range.Items, item => item.Name == "Phone Beta");
        Assert.Contains(range.Items, item => item.Name == "Desk Delta");
    }

    [Theory]
    [InlineData("priceAsc", new[] { "Chair Gamma", "Desk Delta", "Phone Beta", "Laptop Alpha" })]
    [InlineData("priceDesc", new[] { "Laptop Alpha", "Phone Beta", "Desk Delta", "Chair Gamma" })]
    [InlineData("nameAsc", new[] { "Chair Gamma", "Desk Delta", "Laptop Alpha", "Phone Beta" })]
    [InlineData("newest", new[] { "Desk Delta", "Chair Gamma", "Phone Beta", "Laptop Alpha" })]
    public async Task Sorting_IsAppliedByTheBackend(string sortBy, string[] expectedNames)
    {
        var page = await GetProductsAsync($"?sortBy={sortBy}");

        Assert.Equal(expectedNames, page.Items.Select(item => item.Name).ToArray());
    }

    [Fact]
    public async Task Pagination_ReturnsCorrectPageMetadataAndItems()
    {
        var first = await GetProductsAsync("?pageNumber=1&pageSize=2&sortBy=newest");
        var second = await GetProductsAsync("?pageNumber=2&pageSize=2&sortBy=newest");

        Assert.Equal(4, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(2, first.Items.Count);
        Assert.Equal(new[] { "Desk Delta", "Chair Gamma" }, first.Items.Select(item => item.Name));

        Assert.Equal(2, second.Items.Count);
        Assert.Equal(new[] { "Phone Beta", "Laptop Alpha" }, second.Items.Select(item => item.Name));
    }

    [Theory]
    [InlineData("?pageNumber=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=61")]
    [InlineData("?minPrice=200&maxPrice=100")]
    [InlineData("?sortBy=unknown")]
    public async Task InvalidParameters_ReturnBadRequest(string query)
    {
        var response = await _client.GetAsync($"/api/marketplace/products{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmptyResults_ReturnAnEmptyPage()
    {
        var page = await GetProductsAsync("?search=does-not-exist");

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
        Assert.Equal(1, page.PageNumber);
    }

    [Fact]
    public async Task CategoriesEndpoint_ReturnsOnlyCategoriesWithPublicProducts()
    {
        var categories = await _client.GetFromJsonAsync<List<MarketplaceCategoryDto>>(
            "/api/marketplace/categories");

        Assert.NotNull(categories);
        Assert.Equal(new[] { "Electronics", "Home & Living" }, categories!.Select(c => c.Name));
        Assert.All(categories, category => Assert.True(category.ProductCount > 0));
    }

    private async Task<PagedResponse<MarketplaceProductDto>> GetProductsAsync(string query)
    {
        var page = await _client.GetFromJsonAsync<PagedResponse<MarketplaceProductDto>>(
            $"/api/marketplace/products{query}");

        Assert.NotNull(page);
        return page!;
    }

    private async Task SeedCatalogAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Products.AnyAsync())
        {
            return;
        }

        var electronics = new Category
        {
            Id = 1,
            Name = "Electronics",
            Slug = "electronics",
            SortOrder = 1,
        };
        var home = new Category
        {
            Id = 2,
            Name = "Home & Living",
            Slug = "home-living",
            SortOrder = 2,
        };
        var inactiveCategory = new Category
        {
            Id = 3,
            Name = "Hidden Category",
            Slug = "hidden-category",
            IsActive = false,
            SortOrder = 3,
        };
        var approvedSeller = new SellerProfile
        {
            Id = 1,
            StoreName = "Approved Store",
            Slug = "approved-store",
            IsApproved = true,
            IsActive = true,
        };
        var pendingSeller = new SellerProfile
        {
            Id = 2,
            StoreName = "Pending Store",
            Slug = "pending-store",
            IsApproved = false,
            IsActive = true,
        };

        var country = new Country { Name = "Test Country", Code = "TC" };
        var city = new City { Name = "Test City", Country = country };
        var warehouse = new Warehouse
        {
            Name = "Catalog Test Warehouse",
            Address = "1 Test Street",
            City = city,
            Country = country,
        };

        var products = new[]
        {
            CreateProduct(
                "Laptop Alpha",
                "laptop-alpha",
                "LAP-1",
                "Powerful laptop for work.",
                1000m,
                900m,
                electronics,
                approvedSeller,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateProduct(
                "Phone Beta",
                "phone-beta",
                "PHONE-1",
                "A connected smartphone.",
                500m,
                null,
                electronics,
                approvedSeller,
                new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreateProduct(
                "Chair Gamma",
                "chair-gamma",
                "CHAIR-1",
                "An ergonomic chair for long days.",
                200m,
                150m,
                home,
                approvedSeller,
                new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)),
            CreateProduct(
                "Desk Delta",
                "desk-delta",
                "DESK-1",
                "A standing desk with a compact footprint.",
                300m,
                null,
                home,
                null,
                new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc)),
            CreateProduct(
                "Hidden Draft",
                "hidden-draft",
                "HID-1",
                "Not published.",
                100m,
                null,
                electronics,
                approvedSeller,
                new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                isPublished: false),
            CreateProduct(
                "Pending Product",
                "pending-product",
                "PEN-1",
                "Not approved.",
                100m,
                null,
                electronics,
                approvedSeller,
                new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc),
                approvalStatus: ProductApprovalStatus.PendingApproval),
            CreateProduct(
                "Inactive Product",
                "inactive-product",
                "INA-1",
                "Not active.",
                100m,
                null,
                electronics,
                approvedSeller,
                new DateTime(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc),
                isActive: false),
            CreateProduct(
                "Unapproved Seller Product",
                "unapproved-seller-product",
                "UNA-1",
                "Seller is not approved.",
                100m,
                null,
                electronics,
                pendingSeller,
                new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc)),
            CreateProduct(
                "Inactive Category Product",
                "inactive-category-product",
                "INC-1",
                "Category is not active.",
                100m,
                null,
                inactiveCategory,
                approvedSeller,
                new DateTime(2026, 1, 9, 0, 0, 0, DateTimeKind.Utc)),
        };

        var stockBySku = new Dictionary<string, int>
        {
            ["LAP-1"] = 5,
            ["PHONE-1"] = 0,
            ["CHAIR-1"] = 2,
            ["DESK-1"] = 4,
            ["HID-1"] = 1,
            ["PEN-1"] = 1,
            ["INA-1"] = 1,
            ["UNA-1"] = 1,
            ["INC-1"] = 1,
        };

        db.Categories.AddRange(electronics, home, inactiveCategory);
        db.SellerProfiles.AddRange(approvedSeller, pendingSeller);
        db.Products.AddRange(products);

        foreach (var product in products)
        {
            db.WarehouseItems.Add(new WarehouseItem
            {
                ItemName = product.Name,
                SKUCode = product.Sku,
                Qty = stockBySku.GetValueOrDefault(product.Sku ?? string.Empty),
                CostPrice = product.Price * 0.6m,
                MSRPPrice = product.Price,
                Warehouse = warehouse,
                Product = product,
            });
        }

        await db.SaveChangesAsync();
    }

    private static Product CreateProduct(
        string name,
        string slug,
        string sku,
        string description,
        decimal price,
        decimal? discountPrice,
        Category category,
        SellerProfile? seller,
        DateTime createdUtc,
        bool isPublished = true,
        bool isActive = true,
        ProductApprovalStatus approvalStatus = ProductApprovalStatus.Approved)
    {
        return new Product
        {
            Name = name,
            Slug = slug,
            Sku = sku,
            Description = description,
            Price = price,
            DiscountPrice = discountPrice,
            Category = category,
            SellerProfile = seller,
            IsPublished = isPublished,
            IsActive = isActive,
            ApprovalStatus = approvalStatus,
            CreatedUtc = createdUtc,
        };
    }
}
