using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SmartDine.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    public static readonly RSA RsaKey;
    public static readonly string RsaPrivateKeyBase64;
    public static readonly string RsaPublicKeyBase64;

    static CustomWebApplicationFactory()
    {
        RsaKey = RSA.Create(2048);
        RsaPrivateKeyBase64 = Convert.ToBase64String(RsaKey.ExportRSAPrivateKey());
        RsaPublicKeyBase64 = Convert.ToBase64String(RsaKey.ExportRSAPublicKey());

        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // InMemory config added LAST → overrides appsettings.*.json
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "UseInMemoryDatabase", "true" },
                { "Jwt:RsaPrivateKey", RsaPrivateKeyBase64 },
                { "Jwt:RsaPublicKey", RsaPublicKeyBase64 },
                { "Jwt:Issuer", "SmartDineAPI" },
                { "Jwt:Audience", "SmartDineApp" },
                { "Jwt:AccessTokenExpiryMinutes", "60" },
                { "Jwt:RefreshTokenExpiryDays", "7" }
            });
        });
    }
}
