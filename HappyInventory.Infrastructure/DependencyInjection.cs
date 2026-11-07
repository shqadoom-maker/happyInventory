using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Infrastructure.Data;
using HappyInventory.Infrastructure.Repositories;
using HappyInventory.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HappyInventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is required. Configure it in appsettings.json, " +
                "User Secrets, or an environment variable.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<UserRepository>();
        services.AddScoped<IUserRepository>(provider => provider.GetRequiredService<UserRepository>());

        services.AddScoped<RefreshTokenRepository>();
        services.AddScoped<IRefreshTokenRepository>(provider =>
            provider.GetRequiredService<RefreshTokenRepository>());

        services.AddScoped<CountryRepository>();
        services.AddScoped<ICountryRepository>(provider => provider.GetRequiredService<CountryRepository>());

        services.AddScoped<CityRepository>();
        services.AddScoped<ICityRepository>(provider => provider.GetRequiredService<CityRepository>());

        services.AddScoped<WarehouseRepository>();
        services.AddScoped<IWarehouseRepository>(provider => provider.GetRequiredService<WarehouseRepository>());

        services.AddScoped<WarehouseItemRepository>();
        services.AddScoped<IWarehouseItemRepository>(provider => provider.GetRequiredService<WarehouseItemRepository>());

        services.AddScoped<InventoryUpdateRepository>();
        services.AddScoped<IInventoryUpdateRepository>(provider => provider.GetRequiredService<InventoryUpdateRepository>());

        services.AddScoped<DashboardRepository>();
        services.AddScoped<IDashboardRepository>(provider => provider.GetRequiredService<DashboardRepository>());

        services.AddScoped<SystemLogRepository>();
        services.AddScoped<ISystemLogRepository>(provider => provider.GetRequiredService<SystemLogRepository>());

        services.AddScoped<MarketplaceCatalogRepository>();
        services.AddScoped<IMarketplaceCatalogRepository>(provider =>
            provider.GetRequiredService<MarketplaceCatalogRepository>());

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
