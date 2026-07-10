using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace SmartDine.Application;

/// <summary>
/// Extension method đăng ký tất cả services của Application layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Services
        services.AddScoped<Services.AuthService>();
        services.AddScoped<Services.OrderService>();
        services.AddScoped<Services.MenuService>();
        services.AddScoped<Services.TableService>();
        services.AddScoped<Services.DiningSessionService>();
        services.AddScoped<Services.PaymentService>();
        services.AddScoped<Services.StaffService>();
        services.AddScoped<Services.MenuCategoryService>();
        services.AddScoped<Services.SettingsService>();

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
