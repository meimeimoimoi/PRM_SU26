using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace SmartDine.Infrastructure.Security;

public class RsaKeyService
{
    private readonly RSA _rsa;

    public RsaKeyService(IConfiguration configuration)
    {
        _rsa = RSA.Create();

        var privateKeyBase64 = configuration["Jwt:RsaPrivateKey"];
        if (!string.IsNullOrEmpty(privateKeyBase64))
        {
            var privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
            _rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
        }
        else
        {
            _rsa = RSA.Create(2048);
        }
    }

    public RSA GetRsaKey() => _rsa;

    public static (string privateKeyBase64, string publicKeyBase64) GenerateKeyPair()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        return (privateKey, publicKey);
    }

    public static RSA LoadPublicKey(string publicKeyBase64)
    {
        var rsa = RSA.Create();
        var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
        rsa.ImportRSAPublicKey(publicKeyBytes, out _);
        return rsa;
    }
}
