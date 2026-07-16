using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IRsaKeyProvider _keyProvider;

    public JwtTokenService(IConfiguration configuration, IRsaKeyProvider keyProvider)
    {
        _configuration = configuration;
        _keyProvider = keyProvider;
    }

    public (string Token, string JwtId) GenerateAccessToken(int id, string email, string fullName, string role)
    {
        var jwtId = Guid.NewGuid().ToString("N");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, jwtId),
        };

        var rsa = RSA.Create();
        rsa.ImportParameters(_keyProvider.PrivateKeyParameters);
        var creds = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

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

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public (string Token, string JwtId) GenerateGuestToken(string guestUniqueId, int sessionId, string guestName)
    {
        var jwtId = Guid.NewGuid().ToString("N");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, guestUniqueId),
            new Claim("session_id", sessionId.ToString()),
            new Claim(ClaimTypes.Name, guestName),
            new Claim(ClaimTypes.Role, "GUEST"),
            new Claim(JwtRegisteredClaimNames.Jti, jwtId),
        };

        var rsa = RSA.Create();
        rsa.ImportParameters(_keyProvider.PrivateKeyParameters);
        var creds = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

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

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();

        // Kiểm tra cấu trúc chuỗi trước. Nếu không phải định dạng JWT, trả về null.
        if (!tokenHandler.CanReadToken(token))
        {
            return null;
        }

        // CanReadToken có thể true với chuỗi 3 đoạn không Base64url được — đọc thử để trả null (chuỗi rác).
        try
        {
            _ = tokenHandler.ReadJwtToken(token);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }

        using var rsa = RSA.Create();
        rsa.ImportParameters(_keyProvider.PublicKeyParameters);
        // Copy parameters so RsaSecurityKey không phụ thuộc RSA bị dispose sau using.
        var signingKey = new RsaSecurityKey(rsa.ExportParameters(false));

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = signingKey
        };

        // ValidateToken sẽ ném SecurityTokenException nếu chữ ký sai hoặc sai thuật toán
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256,
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token algorithm");
        }

        return principal;
    }

    public string GeneratePasswordResetToken(int id, string email, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("purpose", "password-reset"),
        };

        var rsa = RSA.Create();
        rsa.ImportParameters(_keyProvider.PrivateKeyParameters);
        var creds = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidatePasswordResetToken(string token)
    {
        var rsa = RSA.Create();
        rsa.ImportParameters(_keyProvider.PublicKeyParameters);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParams, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                return null;

            if (principal.FindFirst("purpose")?.Value != "password-reset")
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
