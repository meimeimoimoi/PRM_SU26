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
        var privatePem = File.ReadAllText(configuration["Jwt:PrivateKeyPath"]!);
        using var rsaPrivate = RSA.Create();
        rsaPrivate.ImportFromPem(privatePem);
        PrivateKeyParameters = rsaPrivate.ExportParameters(true);

        var publicPem = File.ReadAllText(configuration["Jwt:PublicKeyPath"]!);
        using var rsaPublic = RSA.Create();
        rsaPublic.ImportFromPem(publicPem);
        PublicKeyParameters = rsaPublic.ExportParameters(false);
    }
}
