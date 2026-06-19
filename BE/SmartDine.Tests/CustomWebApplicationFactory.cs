using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "InMemoryDbForTesting");
        Environment.SetEnvironmentVariable("Jwt__RsaPrivateKey", RsaPrivateKeyBase64);
        Environment.SetEnvironmentVariable("Jwt__RsaPublicKey", RsaPublicKeyBase64);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "SmartDineAPI");
        Environment.SetEnvironmentVariable("Jwt__Audience", "SmartDineApp");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
        Environment.SetEnvironmentVariable("Jwt__RefreshTokenExpiryDays", "7");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
    }
}
