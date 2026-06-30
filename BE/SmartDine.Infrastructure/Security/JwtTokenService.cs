using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartDine.Domain.Constants;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Security;

/// <summary>
/// Implement IJwtTokenService — tạo và validate JWT token bằng RSA256.
///
/// Cấu trúc AccessToken (JWT):
///   Header:  { alg: "RS256", typ: "JWT" }
///   Payload: { jti, nameid (userId), email, name, role, iss, aud, exp }
///   Signature: RSA-SHA256 (private key ký, public key verify)
///
/// Config cần thiết (appsettings.json):
///   Jwt:Issuer                  → "SmartDineAPI"
///   Jwt:Audience                → "SmartDineApp"
///   Jwt:AccessTokenExpiryMinutes → 60 (mặc định)
///   Jwt:RsaPrivateKey           → base64 RSA private key (chỉ Identity.API)
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly RsaKeyService _rsaKeyService;

    public JwtTokenService(IConfiguration configuration, RsaKeyService rsaKeyService)
    {
        _configuration = configuration;
        _rsaKeyService = rsaKeyService;
    }

    /// <summary>
    /// Tạo JWT AccessToken với RSA256 signing.
    ///
    /// Luồng:
    ///   1. Sinh jwtId (GUID) → dùng làm claim jti, liên kết với RefreshToken trong DB.
    ///   2. Tạo claims array: jti, userId, email, fullName, role.
    ///   3. Lấy RSA private key từ RsaKeyService → tạo SigningCredentials (RS256).
    ///   4. Build JwtSecurityToken với issuer, audience, claims, expiry.
    ///   5. Serialize thành JWT string (header.payload.signature).
    ///
    /// Trả về: (token string, jwtId) — caller lưu jwtId vào RefreshToken.JwtId.
    /// </summary>
    public (string token, string jwtId) GenerateAccessToken(int id, string email, string fullName, string role)
    {
        var jwtId = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, jwtId),
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Role, role),
        };

        var rsaKey = _rsaKeyService.GetRsaKey();
        var key = new RsaSecurityKey(rsaKey);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), jwtId);
    }

    /// <summary>
    /// Tạo JWT cho GUEST với UUID làm sub — phân biệt được từng GUEST tại cùng một bàn.
    /// Thêm custom claim "session_id" để service lấy sessionId khi cần mà không cần parse sub.
    /// </summary>
    public (string token, string jwtId) GenerateGuestToken(string guestUniqueId, int sessionId, string guestName)
    {
        var jwtId = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, jwtId),
            new Claim(ClaimTypes.NameIdentifier, guestUniqueId),
            new Claim(JwtClaimTypes.SessionId, sessionId.ToString()),
            new Claim(ClaimTypes.Name, guestName),
            new Claim(ClaimTypes.Role, "GUEST"),
        };

        var rsaKey = _rsaKeyService.GetRsaKey();
        var key = new RsaSecurityKey(rsaKey);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:AccessTokenExpiryMinutes"] ?? "60")),
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), jwtId);
    }

    /// <summary>
    /// Tạo refresh token: 64 bytes random → base64 (86 ký tự).
    /// Cryptographically secure (RandomNumberGenerator), không thể đoán được.
    /// Không phải JWT — chỉ là opaque string lưu trong DB, client gửi lại khi cần renew.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Tạo password reset token: 32 bytes random → base64 (44 ký tự).
    /// Ngắn hơn refresh token vì single-use và hết hạn nhanh (15 phút).
    /// </summary>
    public string GeneratePasswordResetToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Parse JWT đã hết hạn → trích xuất claims mà KHÔNG yêu cầu token còn hiệu lực.
    ///
    /// Luồng:
    ///   1. Lấy RSA key → tạo TokenValidationParameters với ValidateLifetime = false.
    ///   2. ValidateToken: kiểm tra issuer, audience, signature (vẫn verify RSA256).
    ///   3. Double-check: algorithm trong header phải là RS256 (chống algorithm confusion attack).
    ///   4. Trả ClaimsPrincipal chứa claims, hoặc null nếu token bị giả mạo.
    ///
    /// Dùng trong RefreshTokenAsync: cần lấy jti + userId từ expired access token
    /// để match với RefreshToken trong DB trước khi cấp cặp token mới.
    /// </summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var rsaKey = _rsaKeyService.GetRsaKey();
            var key = new RsaSecurityKey(rsaKey);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = key
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            // Chống algorithm confusion: đảm bảo token dùng RS256, không phải HS256 hay none
            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
