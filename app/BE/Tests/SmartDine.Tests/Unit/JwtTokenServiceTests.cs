using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartDine.Domain.Interfaces;
using SmartDine.Infrastructure.Security;

namespace SmartDine.Tests.Unit;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;
    private readonly FakeRsaKeyProvider _keyProvider;
    private readonly IConfiguration _config;

    private const string Issuer   = "TestIssuer";
    private const string Audience = "TestAudience";

    public JwtTokenServiceTests()
    {
        _keyProvider = new FakeRsaKeyProvider();
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"]                   = Issuer,
                ["Jwt:Audience"]                 = Audience,
                ["Jwt:AccessTokenExpiryMinutes"] = "60",
            })
            .Build();

        _sut = new JwtTokenService(_config, _keyProvider);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyToken()
    {
        var (token, jwtId) = _sut.GenerateAccessToken(1, "user@test.com", "Test User", "CUSTOMER");

        Assert.NotEmpty(token);
        Assert.NotEmpty(jwtId);
    }

    [Fact]
    public void GenerateAccessToken_UsesRS256Algorithm()
    {
        var (token, _) = _sut.GenerateAccessToken(1, "user@test.com", "Test User", "CUSTOMER");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(SecurityAlgorithms.RsaSha256, jwt.Header.Alg);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectClaims()
    {
        var (token, _) = _sut.GenerateAccessToken(42, "user@test.com", "Test User", "MANAGER");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("42",            jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("user@test.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Test User",     jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("MANAGER",       jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectIssuerAndAudience()
    {
        var (token, _) = _sut.GenerateAccessToken(1, "user@test.com", "Test User", "STAFF");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(Issuer,   jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresIn60Minutes()
    {
        var before = DateTime.UtcNow;
        var (token, _) = _sut.GenerateAccessToken(1, "user@test.com", "Test User", "CUSTOMER");
        var after  = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(jwt.ValidTo >= before.AddMinutes(59));
        Assert.True(jwt.ValidTo <= after.AddMinutes(61));
    }

    [Fact]
    public void GenerateAccessToken_IsVerifiableWithPublicKey()
    {
        var (token, _) = _sut.GenerateAccessToken(1, "user@test.com", "Test User", "CUSTOMER");

        var rsa = RSA.Create();
        rsa.ImportParameters(_keyProvider.PublicKeyParameters);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = Issuer,
            ValidAudience            = Audience,
            IssuerSigningKey         = new RsaSecurityKey(rsa),
            ClockSkew                = TimeSpan.Zero
        };

        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(token, validationParams, out _);

        Assert.NotNull(principal);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64String()
    {
        var refreshToken = _sut.GenerateRefreshToken();

        Assert.NotNull(refreshToken);
        var bytes = Convert.FromBase64String(refreshToken);
        Assert.Equal(64, bytes.Length);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentValueEachCall()
    {
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ReturnsClaimsForExpiredToken()
    {
        var expiredConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"]                   = Issuer,
                ["Jwt:Audience"]                 = Audience,
                ["Jwt:AccessTokenExpiryMinutes"] = "-1",
            })
            .Build();
        var service      = new JwtTokenService(expiredConfig, _keyProvider);
        var (expiredToken, _) = service.GenerateAccessToken(99, "expired@test.com", "Expired User", "STAFF");

        var principal = _sut.GetPrincipalFromExpiredToken(expiredToken);

        Assert.NotNull(principal);
        Assert.Equal("99", principal!.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("expired@test.com", principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ThrowsForTamperedToken()
    {
        var (token, _) = _sut.GenerateAccessToken(1, "user@test.com", "Test", "CUSTOMER");
        var tampered = token[..^5] + "XXXXX";

        Assert.ThrowsAny<SecurityTokenException>(
            () => _sut.GetPrincipalFromExpiredToken(tampered));
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ThrowsForHS256Token()
    {
        var hmacKey    = new SymmetricSecurityKey(new byte[32]);
        var hs256Creds = new SigningCredentials(hmacKey, SecurityAlgorithms.HmacSha256);
        var hs256Token = new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(Issuer, Audience,
                signingCredentials: hs256Creds,
                expires: DateTime.UtcNow.AddHours(-1)));

        Assert.ThrowsAny<Exception>(() => _sut.GetPrincipalFromExpiredToken(hs256Token));
    }

    [Fact]
    public void GenerateAccessToken_DifferentCallsProduceDifferentJwtIds()
    {
        var (_, jwtId1) = _sut.GenerateAccessToken(1, "user@test.com", "Test", "STAFF");
        var (_, jwtId2) = _sut.GenerateAccessToken(1, "user@test.com", "Test", "STAFF");

        Assert.NotEqual(jwtId1, jwtId2);
    }
}
