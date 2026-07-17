using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SmartDine.Infrastructure.Security;

namespace SmartDine.Tests.Unit;

public class RsaKeyProviderTests
{
    private readonly string _privateKeyBase64;
    private readonly string _publicKeyBase64;

    public RsaKeyProviderTests()
    {
        using var rsa = RSA.Create(2048);
        _privateKeyBase64 = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        _publicKeyBase64 = Convert.ToBase64String(rsa.ExportRSAPublicKey());
    }

    private IConfiguration BuildConfig(string? privateKey = null, string? publicKey = null)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:RsaPrivateKey"] = privateKey ?? _privateKeyBase64,
                ["Jwt:RsaPublicKey"] = publicKey ?? _publicKeyBase64,
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
    public void Constructor_ThrowsWhenPrivateKeyMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:RsaPublicKey"] = _publicKeyBase64,
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new RsaKeyProvider(config));
    }

    [Fact]
    public void Constructor_ThrowsWhenPublicKeyMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:RsaPrivateKey"] = _privateKeyBase64,
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new RsaKeyProvider(config));
    }
}
