using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartDine.Domain.Interfaces;
using SmartDine.Infrastructure.ExternalServices;
using SmartDine.Infrastructure.Persistence;
using SmartDine.Infrastructure.Persistence.Repositories;
using SmartDine.Infrastructure.Security;

namespace SmartDine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL + EF Core
        if (configuration["UseInMemoryDatabase"] == "true")
        {
            services.AddDbContext<SmartDineDbContext>(options =>
                options.UseInMemoryDatabase("SmartDineTestDb"));
        }
        else
        {
            services.AddDbContext<SmartDineDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ITableRepository, TableRepository>();
        services.AddScoped<IDiningSessionRepository, DiningSessionRepository>();
        services.AddScoped<ITableReservationRepository, TableReservationRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();


        // Security
        services.AddSingleton<IRsaKeyProvider, RsaKeyProvider>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        // Fallback services
        services.AddScoped<IOrderNotificationService, Services.NullOrderNotificationService>();

        // IPaymentGateway: fallback no-op cho test/monolith. Order.API ghi đè bằng PayOsGateway.
        services.AddScoped<IPaymentGateway, Services.NullPaymentGateway>();

        // Seeder
        services.AddScoped<DbSeeder>();

        return services;
    }
}
