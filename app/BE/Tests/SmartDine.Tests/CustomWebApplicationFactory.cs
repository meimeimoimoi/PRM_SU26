extern alias MenuApi;
extern alias OrderApi;
extern alias TableApi;

using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MenuApi::SmartDine.Menu.API.Controllers;
using OrderApi::SmartDine.Order.API.Controllers;
using TableApi::SmartDine.Table.API.Controllers;

namespace SmartDine.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    static CustomWebApplicationFactory()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());

        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("Jwt__RsaPrivateKey", privateKey);
        Environment.SetEnvironmentVariable("Jwt__RsaPublicKey", publicKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "SmartDineTest");
        Environment.SetEnvironmentVariable("Jwt__Audience", "SmartDineTest");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenExpiryDays", "7");

        // Tránh WebApplicationFactory quét *.sln (repo dùng .slnx → fail trên CI).
        var assemblyName = typeof(TProgram).Assembly.GetName().Name!;
        var settingSuffix = assemblyName.ToUpperInvariant().Replace(".", "_", StringComparison.Ordinal);
        Environment.SetEnvironmentVariable(
            $"ASPNETCORE_TEST_CONTENTROOT_{settingSuffix}",
            AppContext.BaseDirectory);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Đặt ContentRoot trước SetContentRoot nội bộ (ConfigureWebHost chạy sau, không kịp).
        var assemblyName = typeof(TProgram).Assembly.GetName().Name!;
        var settingSuffix = assemblyName.ToUpperInvariant().Replace(".", "_", StringComparison.Ordinal);
        builder.ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseSetting($"TEST_CONTENTROOT_{settingSuffix}", AppContext.BaseDirectory);
            webBuilder.UseContentRoot(AppContext.BaseDirectory);
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(AppContext.BaseDirectory);
        builder.UseEnvironment("Development");

        // Identity.API chỉ có Auth — đăng ký thêm controller Menu/Order/Table cho integration flow.
        builder.ConfigureServices(services =>
        {
            services.AddControllers()
                .AddApplicationPart(typeof(TablesController).Assembly)
                .AddApplicationPart(typeof(MenuItemsController).Assembly)
                .AddApplicationPart(typeof(OrdersController).Assembly);
        });
    }
}
