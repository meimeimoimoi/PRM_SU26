using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace SmartDine.Infrastructure.Security;

/// <summary>
/// Singleton service quản lý RSA key pair cho JWT signing.
///
/// Khởi tạo:
///   - Nếu config có Jwt:RsaPrivateKey (base64) → import private key từ config.
///     Dùng cho Identity.API (service duy nhất cần ký token).
///   - Nếu không có → tự sinh RSA 2048-bit mới (dùng cho dev/test).
///
/// Kiến trúc key distribution trong microservice:
///   - Identity.API: config chứa PRIVATE key → dùng để ký JWT.
///   - Menu/Order/Table/AI API: config chứa PUBLIC key → dùng để verify JWT.
///   - Public key được extract từ private key, encode base64, share cho các service khác.
///
/// Singleton vì RSA key không đổi trong suốt lifetime của application.
/// </summary>
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

    /// <summary>Trả về RSA instance chứa private key (hoặc auto-generated key).</summary>
    public RSA GetRsaKey() => _rsa;

    /// <summary>
    /// Utility: sinh cặp RSA 2048-bit key mới (private + public) dưới dạng base64.
    /// Dùng 1 lần khi setup project → copy vào appsettings.json.
    /// </summary>
    public static (string privateKeyBase64, string publicKeyBase64) GenerateKeyPair()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        return (privateKey, publicKey);
    }

    /// <summary>
    /// Utility: load RSA public key từ base64 string.
    /// Dùng trong Program.cs của các service (Menu, Order, Table, AI) để verify JWT.
    /// </summary>
    public static RSA LoadPublicKey(string publicKeyBase64)
    {
        var rsa = RSA.Create();
        var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
        rsa.ImportRSAPublicKey(publicKeyBytes, out _);
        return rsa;
    }
}
