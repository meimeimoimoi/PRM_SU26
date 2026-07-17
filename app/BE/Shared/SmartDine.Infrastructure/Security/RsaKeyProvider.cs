using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Security;

public class RsaKeyProvider : IRsaKeyProvider
{
    public RSAParameters PrivateKeyParameters { get; }
    public RSAParameters PublicKeyParameters { get; }

    public RsaKeyProvider(IConfiguration configuration)
    {
        var privateKeyBase64 = configuration["Jwt:RsaPrivateKey"]
            ?? throw new InvalidOperationException("Missing configuration value 'Jwt:RsaPrivateKey'.");
        using var rsaPrivate = RSA.Create();
        rsaPrivate.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
        PrivateKeyParameters = rsaPrivate.ExportParameters(true);

        var publicKeyBase64 = configuration["Jwt:RsaPublicKey"]
            ?? throw new InvalidOperationException("Missing configuration value 'Jwt:RsaPublicKey'.");
        using var rsaPublic = RSA.Create();
        rsaPublic.ImportRSAPublicKey(Convert.FromBase64String(publicKeyBase64), out _);
        PublicKeyParameters = rsaPublic.ExportParameters(false);
    }
}
