using Bariza.Application.Interfaces.Services;
using Bariza.Core.Entities;
using Bariza.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bariza.Infrastructure.Data;

public static class DbSeeder
{
    private const string LegacyUserRole = "User";

    public static Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        return SeedAsync(services, configuration, seedMarketplaceCatalog: false);
    }

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        bool seedMarketplaceCatalog)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DbSeeder));

        await db.Database.MigrateAsync();
        await EnsureDefaultAdminAsync(db, hasher, configuration, logger);
        await ClearLegacyPasswordChangeFlagsAsync(db, logger);
        await NormalizeLegacyRolesAsync(db, logger);

        if (seedMarketplaceCatalog)
        {
            await SeedMarketplaceCatalogAsync(db, hasher, logger);
        }
    }

    private static async Task EnsureDefaultAdminAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        IConfiguration configuration,
        ILogger logger)
    {
        var username = configuration["Seed:AdminUsername"]?.Trim();
        var fullName = configuration["Seed:AdminFullName"]?.Trim();

        username = string.IsNullOrWhiteSpace(username) ? "admin@happywarehouse.com" : username;
        fullName = string.IsNullOrWhiteSpace(fullName) ? "System Administrator" : fullName;

        var normalized = username.ToLowerInvariant();

        if (await db.Users.AnyAsync(user => user.NormalizedUsername == normalized))
        {
            // Administrator already exists. Do not reset the password or
            // account details on subsequent startups. Legacy password-change
            // flags are cleared separately below.
            return;
        }

        // A newly created administrator must have a known password. Refuse to
        // generate an unrecoverable password that is never shown to an operator.
        var configuredPassword = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            throw new InvalidOperationException(
                $"No administrator account exists for '{username}', and Seed:AdminPassword is not configured. " +
                "Set Seed:AdminPassword to a strong password before starting the application.");
        }

        db.Users.Add(CreateDefaultAdmin(username, normalized, fullName, configuredPassword, hasher));
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seeded default administrator account '{Username}'. Change the password after signing in.",
            username);
    }

    private static User CreateDefaultAdmin(
        string username,
        string normalized,
        string fullName,
        string password,
        IPasswordHasher hasher) => new()
    {
        Username = username,
        NormalizedUsername = normalized,
        FullName = fullName,
        Role = UserRoles.Admin,
        IsActive = true,
        MustChangePassword = false,
        PasswordHash = hasher.Hash(password),
        CreatedUtc = DateTime.UtcNow,
    };

    private static async Task ClearLegacyPasswordChangeFlagsAsync(AppDbContext db, ILogger logger)
    {
        // Older seeded administrators may still have MustChangePassword = true.
        // Clear the legacy flag without touching their password or other data.
        var users = await db.Users
            .Where(user => user.MustChangePassword)
            .ToListAsync();

        if (users.Count == 0)
        {
            return;
        }

        foreach (var user in users)
        {
            user.MustChangePassword = false;
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Cleared the legacy first-login password-change flag for {Count} account(s).",
            users.Count);
    }

    private static async Task NormalizeLegacyRolesAsync(AppDbContext db, ILogger logger)
    {
        var legacyUsers = await db.Users
            .Where(user => user.Role == LegacyUserRole)
            .ToListAsync();

        if (legacyUsers.Count == 0)
        {
            return;
        }

        foreach (var user in legacyUsers)
        {
            user.Role = UserRoles.Auditor;
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Mapped {Count} legacy 'User' role account(s) to '{Role}'.",
            legacyUsers.Count,
            UserRoles.Auditor);
    }

    private static async Task SeedMarketplaceCatalogAsync(AppDbContext db, IPasswordHasher hasher, ILogger logger)
    {
        // Demo catalog data is only used by the Development host. It is skipped
        // as soon as a product exists, so a developer's edits are never reset.
        if (await db.Products.AnyAsync())
        {
            return;
        }

        var electronics = new Category
        {
            Name = "Electronics",
            Slug = "electronics",
            Description = "Laptops, audio, wearables and everyday technology.",
            SortOrder = 1,
        };
        var home = new Category
        {
            Name = "Home & Living",
            Slug = "home-living",
            Description = "Furniture and thoughtful pieces for the home.",
            SortOrder = 2,
        };
        var fashion = new Category
        {
            Name = "Fashion",
            Slug = "fashion",
            Description = "Everyday clothing and accessories.",
            SortOrder = 3,
        };
        var sports = new Category
        {
            Name = "Sports & Outdoors",
            Slug = "sports-outdoors",
            Description = "Gear for movement and the outdoors.",
            SortOrder = 4,
        };

        var novaTech = new SellerProfile
        {
            StoreName = "Nova Tech",
            Slug = "nova-tech",
            Description = "Connected devices for work and play.",
            IsApproved = true,
            IsActive = true,
        };
        var casa = new SellerProfile
        {
            StoreName = "Casa & Co.",
            Slug = "casa-and-co",
            Description = "Warm, practical furniture for modern homes.",
            IsApproved = true,
            IsActive = true,
        };
        var urban = new SellerProfile
        {
            StoreName = "Urban Threads",
            Slug = "urban-threads",
            Description = "Everyday essentials with a considered fit.",
            IsApproved = true,
            IsActive = true,
        };
        var pendingSeller = new SellerProfile
        {
            StoreName = "Pending Seller",
            Slug = "pending-seller",
            Description = "Awaiting marketplace approval.",
            IsApproved = false,
            IsActive = true,
        };

        var now = DateTime.UtcNow;
        var products = new[]
        {
            new Product
            {
                Name = "Aurora 14\" Laptop",
                Slug = "aurora-14-laptop",
                Description = "A lightweight laptop with a bright display and all-day battery.",
                Sku = "AUR-LAP-14",
                Price = 1299.00m,
                DiscountPrice = 1149.00m,
                ImageUrl = "/images/marketplace/laptop.svg",
                Category = electronics,
                SellerProfile = novaTech,
                IsPublished = true,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.Approved,
                CreatedUtc = now.AddDays(-8),
            },
            new Product
            {
                Name = "Pulse Wireless Headphones",
                Slug = "pulse-wireless-headphones",
                Description = "Comfortable over-ear headphones with active noise cancellation.",
                Sku = "PUL-HEAD-01",
                Price = 249.00m,
                DiscountPrice = 199.00m,
                ImageUrl = "/images/marketplace/headphones.svg",
                Category = electronics,
                SellerProfile = novaTech,
                IsPublished = true,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.Approved,
                CreatedUtc = now.AddDays(-6),
            },
            new Product
            {
                Name = "Orbit Smart Watch",
                Slug = "orbit-smart-watch",
                Description = "A slim smart watch for daily activity and notifications.",
                Sku = "ORB-WATCH-02",
                Price = 399.00m,
                ImageUrl = "/images/marketplace/watch.svg",
                Category = electronics,
                SellerProfile = novaTech,
                IsPublished = true,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.Approved,
                CreatedUtc = now.AddDays(-4),
            },
            new Product
            {
                Name = "Oakline Study Desk",
                Slug = "oakline-study-desk",
                Description = "A solid oak desk with a clean profile and cable management.",
                Sku = "OAK-DESK-01",
                Price = 549.00m,
                DiscountPrice = 499.00m,
                ImageUrl = "/images/marketplace/desk.svg",
                Category = home,
                SellerProfile = casa,
                IsPublished = true,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.Approved,
                CreatedUtc = now.AddDays(-7),
            },
            new Product
            {
                Name = "Cloud Comfort Sofa",
                Slug = "cloud-comfort-sofa",
                Description = "A deep three-seat sofa with a soft, structured weave.",
                Sku = "CLOUD-SOFA-02",
                Price = 1899.00m,
                ImageUrl = "/images/marketplace/sofa.svg",
                Category = home,
                SellerProfile = casa,
                IsPublished = true,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.Approved,
                CreatedUtc = now.AddDays(-5),
            },
            new Product
            {
                Name = "Linen Blend Shirt",
                Slug = "linen-blend-shirt",
                Description = "A breathable linen-blend shirt with a relaxed everyday cut.",
                Sku = "LIN-SHIRT-01",
                Price = 89.00m,
                DiscountPrice = 69.00m,
                ImageUrl = "/images/marketplace/shirt.svg",
                Category = fashion,
                SellerProfile = urban,
                IsPublished = true,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.Approved,
                CreatedUtc = now.AddDays(-3),
            },
            new Product
            {
                Name = "Everyday Canvas Sneakers",
                Slug = "everyday-canvas-sneakers",
                Description = "Classic canvas sneakers made for all-day wear.",
                Sku = "CAN-SNEAK-02",
                Price = 119.00m,
                ImageUrl = "/images/marketplace/sneakers.svg",
                Category = fashion,
                SellerProfile = urban,
                IsPublished = true,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.Approved,
                CreatedUtc = now.AddDays(-2),
            },
            new Product
            {
                Name = "Trail Runner Backpack",
                Slug = "trail-runner-backpack",
                Description = "A weather-resistant daypack with a ventilated back panel.",
                Sku = "TRAIL-BAG-01",
                Price = 149.00m,
                ImageUrl = "/images/marketplace/backpack.svg",
                Category = sports,
                SellerProfile = urban,
                IsPublished = true,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.Approved,
                CreatedUtc = now.AddDays(-1),
            },
            new Product
            {
                Name = "Studio Desk Lamp",
                Slug = "studio-desk-lamp",
                Description = "A future catalog listing that is still being prepared.",
                Sku = "STU-LAMP-01",
                Price = 79.00m,
                ImageUrl = "/images/marketplace/lamp.svg",
                Category = electronics,
                SellerProfile = novaTech,
                IsPublished = false,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.PendingApproval,
                CreatedUtc = now.AddHours(-12),
            },
            new Product
            {
                Name = "Unapproved Seller Speaker",
                Slug = "unapproved-seller-speaker",
                Description = "This product must stay hidden until its seller is approved.",
                Sku = "UNA-SPK-01",
                Price = 99.00m,
                Category = electronics,
                SellerProfile = pendingSeller,
                IsPublished = true,
                IsActive = true,
                ApprovalStatus = ProductApprovalStatus.Approved,
                CreatedUtc = now.AddHours(-10),
            },
        };

        db.Categories.AddRange(electronics, home, fashion, sports);
        db.SellerProfiles.AddRange(novaTech, casa, urban, pendingSeller);
        db.Products.AddRange(products);

        var sellerUser = new User
        {
            Username = "seller@barizamarket.com",
            NormalizedUsername = "SELLER@BARIZAMARKET.COM",
            FullName = "Nova Tech Seller",
            PasswordHash = hasher.Hash("SellerPassword123!"),
            Role = UserRoles.Seller,
            IsActive = true,
            CreatedUtc = now,
        };
        db.Users.Add(sellerUser);
        await db.SaveChangesAsync();

        novaTech.UserId = sellerUser.Id;

        var warehouse = await db.Warehouses
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync();

        if (warehouse is null)
        {
            var country = await db.Countries.FirstOrDefaultAsync(item =>
                item.Code == "US" || item.Name == "United States");

            if (country is null)
            {
                country = new Country { Name = "United States", Code = "US" };
                db.Countries.Add(country);
            }

            var city = await db.Cities.FirstOrDefaultAsync(item =>
                item.CountryId == country.Id && item.Name == "Seattle");

            if (city is null)
            {
                city = new City { Name = "Seattle", Country = country };
                db.Cities.Add(city);
            }

            warehouse = new Warehouse
            {
                Name = "Happy Marketplace Warehouse",
                Address = "500 Market Street",
                City = city,
                Country = country,
            };
            db.Warehouses.Add(warehouse);
        }

        var stockBySku = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["AUR-LAP-14"] = 12,
            ["PUL-HEAD-01"] = 30,
            ["ORB-WATCH-02"] = 0,
            ["OAK-DESK-01"] = 4,
            ["CLOUD-SOFA-02"] = 2,
            ["LIN-SHIRT-01"] = 24,
            ["CAN-SNEAK-02"] = 18,
            ["TRAIL-BAG-01"] = 9,
            ["STU-LAMP-01"] = 5,
            ["UNA-SPK-01"] = 5,
        };

        foreach (var product in products)
        {
            db.WarehouseItems.Add(new WarehouseItem
            {
                ItemName = product.Name,
                SKUCode = product.Sku,
                Qty = stockBySku.GetValueOrDefault(product.Sku ?? string.Empty),
                CostPrice = Math.Round(product.Price * 0.6m, 2),
                MSRPPrice = product.Price,
                Warehouse = warehouse,
                Product = product,
            });
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seeded the development marketplace catalog with {Count} demo products.",
            products.Length);
    }
}
