using Bariza.Application.Interfaces.Services;
using Bariza.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bariza.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<ICountryService, CountryService>();
        services.AddScoped<ICityService, CityService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IWarehouseItemService, WarehouseItemService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IInventoryUpdateService, InventoryUpdateService>();
        services.AddScoped<ISystemLogService, SystemLogService>();
        services.AddScoped<IMarketplaceCatalogService, MarketplaceCatalogService>();

        return services;
    }
}
