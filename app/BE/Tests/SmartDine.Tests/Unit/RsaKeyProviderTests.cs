using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SmartDine.Infrastructure.Security;

namespace SmartDine.Tests.Unit;

public class RsaKeyProviderTests : IDisposable
{
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;

    public RsaKeyProviderTests()
    {
        _privateKeyPath = Path.Combine(Path.GetTempPath(), $"test_private_{Guid.NewGuid()}.pem");
        _publicKeyPath  = Path.Combine(Path.GetTempPath(), $"test_public_{Guid.NewGuid()}.pem");

        using var rsa = RSA.Create(2048);
        File.WriteAllText(_privateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
        File.WriteAllText(_publicKeyPath,  rsa.ExportSubjectPublicKeyInfoPem());
    }

    private IConfiguration BuildConfig(string? privatePath = null, string? publicPath = null)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:PrivateKeyPath"] = privatePath ?? _privateKeyPath,
                ["Jwt:PublicKeyPath"]  = publicPath  ?? _publicKeyPath,
            })
            .Build();

    [Fact]
    public void Constructor_LoadsPrivateAndPublicKey_Successfully()
    {
        var provider = new RsaKeyProvider(BuildConfig());

        Assert.NotEqual(default, provider.PrivateKeyParameters);
        Assert.NotEqual(default, provider.PublicKeyParameters);
    }

    [Fact]
    public void PrivateKeyParameters_ContainsPrivateComponents()
    {
        var provider = new RsaKeyProvider(BuildConfig());

        // Private key must have D (private exponent)
        Assert.NotNull(provider.PrivateKeyParameters.D);
        Assert.NotEmpty(provider.PrivateKeyParameters.D);
    }

    [Fact]
    public void PublicKeyParameters_DoesNotContainPrivateComponents()
    {
        var provider = new RsaKeyProvider(BuildConfig());

        // Public key must NOT have D
        Assert.Null(provider.PublicKeyParameters.D);
    }

    [Fact]
    public void PrivateAndPublicKey_ShareSameModulus()
    {
        var provider = new RsaKeyProvider(BuildConfig());

        // Same key pair → same modulus
        Assert.Equal(provider.PrivateKeyParameters.Modulus, provider.PublicKeyParameters.Modulus);
    }

    [Fact]
    public void Constructor_ThrowsWhenPrivateKeyFileNotFound()
    {
        // Use a valid temp directory but with a non-existent filename → FileNotFoundException
        var missingFile = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid()}.pem");
        var config = BuildConfig(privatePath: missingFile);

        Assert.Throws<FileNotFoundException>(() => new RsaKeyProvider(config));
    }

    [Fact]
    public void Constructor_ThrowsWhenPublicKeyFileNotFound()
    {
        var missingFile = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid()}.pem");
        var config = BuildConfig(publicPath: missingFile);

        Assert.Throws<FileNotFoundException>(() => new RsaKeyProvider(config));
    }

    public void Dispose()
    {
        if (File.Exists(_privateKeyPath)) File.Delete(_privateKeyPath);
        if (File.Exists(_publicKeyPath))  File.Delete(_publicKeyPath);
    }
}
