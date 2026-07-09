using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartDine.Infrastructure.Security;

namespace SmartDine.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtService;
    private readonly RsaKeyService _rsaKeyService;
    private readonly IConfiguration _configuration;

    public JwtTokenServiceTests()
    {
        var rsa = RSA.Create(2048);
        var privateKeyBase64 = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

        var config = new Dictionary<string, string?>
        {
            { "Jwt:RsaPrivateKey", privateKeyBase64 },
            { "Jwt:Issuer", "SmartDineTest" },
            { "Jwt:Audience", "SmartDineTestApp" },
            { "Jwt:AccessTokenExpiryMinutes", "60" }
        };

        _configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
        _rsaKeyService = new RsaKeyService(_configuration);
        _jwtService = new JwtTokenService(_configuration, _rsaKeyService);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwtWithRSA256()
    {
        var (token, jwtId) = _jwtService.GenerateAccessToken(1, "test@test.com", "Test User", "STAFF");

        Assert.NotEmpty(token);
        Assert.NotEmpty(jwtId);

        // Verify it's a valid JWT signed with RS256
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("RS256", jwtToken.Header.Alg);
        Assert.Equal("SmartDineTest", jwtToken.Issuer);
        Assert.Contains(jwtToken.Audiences, a => a == "SmartDineTestApp");
        Assert.Equal(jwtId, jwtToken.Id);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        var (token, _) = _jwtService.GenerateAccessToken(42, "user@example.com", "John Doe", "MANAGER");

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal("42", jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("user@example.com", jwtToken.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("John Doe", jwtToken.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("MANAGER", jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateAccessToken_TokenIsValidatable()
    {
        var (token, _) = _jwtService.GenerateAccessToken(1, "test@test.com", "Test", "STAFF");

        var rsaKey = _rsaKeyService.GetRsaKey();
        var handler = new JwtSecurityTokenHandler();

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "SmartDineTest",
            ValidAudience = "SmartDineTestApp",
            IssuerSigningKey = new RsaSecurityKey(rsaKey)
        };

        var principal = handler.ValidateToken(token, validationParams, out var securityToken);

        Assert.NotNull(principal);
        Assert.IsType<JwtSecurityToken>(securityToken);
        Assert.Equal("RS256", ((JwtSecurityToken)securityToken).Header.Alg);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsUniqueBase64String()
    {
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);

        // Verify it's valid base64
        var bytes = Convert.FromBase64String(token1);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GeneratePasswordResetToken_ReturnsUniqueBase64String()
    {
        var token1 = _jwtService.GeneratePasswordResetToken();
        var token2 = _jwtService.GeneratePasswordResetToken();

        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);

        var bytes = Convert.FromBase64String(token1);
        Assert.Equal(32, bytes.Length);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithValidExpiredToken_ReturnsPrincipal()
    {
        var (token, _) = _jwtService.GenerateAccessToken(1, "test@test.com", "Test", "STAFF");

        var principal = _jwtService.GetPrincipalFromExpiredToken(token);

        Assert.NotNull(principal);
        Assert.Equal("1", principal!.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_WithInvalidToken_ReturnsNull()
    {
        // Tampered token
        var result = _jwtService.GetPrincipalFromExpiredToken("invalid.jwt.token");
        Assert.Null(result);
    }

    [Fact]
    public void GenerateAccessToken_DifferentCallsProduceDifferentJwtIds()
    {
        var (_, jwtId1) = _jwtService.GenerateAccessToken(1, "test@test.com", "Test", "STAFF");
        var (_, jwtId2) = _jwtService.GenerateAccessToken(1, "test@test.com", "Test", "STAFF");

        Assert.NotEqual(jwtId1, jwtId2);
    }
}
