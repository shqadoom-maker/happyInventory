using HappyInventory.Application.Interfaces.Services;
using HappyInventory.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HappyInventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICountryService, CountryService>();
        services.AddScoped<ICityService, CityService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IWarehouseItemService, WarehouseItemService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IInventoryUpdateService, InventoryUpdateService>();
        services.AddScoped<ISystemLogService, SystemLogService>();

        return services;
    }
}
