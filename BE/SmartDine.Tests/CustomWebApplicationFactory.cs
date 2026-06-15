using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartDine.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private static readonly string PrivateKeyPath;
    private static readonly string PublicKeyPath;

    static CustomWebApplicationFactory()
    {
        // Generate temp RSA key pair used for all integration tests
        var tempDir = Path.GetTempPath();
        PrivateKeyPath = Path.Combine(tempDir, "smartdine_test_private.pem");
        PublicKeyPath  = Path.Combine(tempDir, "smartdine_test_public.pem");

        using var rsa = RSA.Create(2048);
        File.WriteAllText(PrivateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
        File.WriteAllText(PublicKeyPath,  rsa.ExportSubjectPublicKeyInfoPem());

        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        Environment.SetEnvironmentVariable("Jwt__PrivateKeyPath", PrivateKeyPath);
        Environment.SetEnvironmentVariable("Jwt__PublicKeyPath",  PublicKeyPath);
        Environment.SetEnvironmentVariable("Jwt__Issuer",   "SmartDineTest");
        Environment.SetEnvironmentVariable("Jwt__Audience", "SmartDineTest");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpiryMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) { }
}
