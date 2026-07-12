using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartDine.Domain.Interfaces;
using SmartDine.Infrastructure.Security;

namespace SmartDine.Tests;

public class FakeRsaKeyProvider : IRsaKeyProvider
{
    public RSAParameters PrivateKeyParameters { get; }
    public RSAParameters PublicKeyParameters { get; }

    public FakeRsaKeyProvider()
    {
        using var rsa = RSA.Create(2048);
        PrivateKeyParameters = rsa.ExportParameters(true);
        PublicKeyParameters = rsa.ExportParameters(false);
    }
}

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtService;
    private readonly IRsaKeyProvider _rsaKeyProvider;
    private readonly IConfiguration _configuration;

    public JwtTokenServiceTests()
    {
        _rsaKeyProvider = new FakeRsaKeyProvider();

        var config = new Dictionary<string, string?>
        {
            { "Jwt:Issuer", "SmartDineTest" },
            { "Jwt:Audience", "SmartDineTestApp" },
            { "Jwt:AccessTokenExpiryMinutes", "60" }
        };

        _configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
        _jwtService = new JwtTokenService(_configuration, _rsaKeyProvider);
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

        var rsa = RSA.Create();
        rsa.ImportParameters(_rsaKeyProvider.PublicKeyParameters);
        var handler = new JwtSecurityTokenHandler();

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "SmartDineTest",
            ValidAudience = "SmartDineTestApp",
            IssuerSigningKey = new RsaSecurityKey(rsa)
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
        var token1 = _jwtService.GeneratePasswordResetToken(1, "test@test.com", "STAFF");
        var token2 = _jwtService.GeneratePasswordResetToken(2, "other@test.com", "CUSTOMER");

        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);
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

    // ═══════════════════════════════════════════════════════════════
    // GenerateGuestToken — JWT cho GUEST (sub = UUID, claim "session_id" riêng)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void GenerateGuestToken_ReturnsValidRs256Jwt()
    {
        var guestUuid = Guid.NewGuid().ToString("N");
        var (token, jwtId) = _jwtService.GenerateGuestToken(guestUuid, 50, "Khách A");

        Assert.NotEmpty(token);
        Assert.NotEmpty(jwtId);

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("RS256", jwtToken.Header.Alg);
        Assert.Equal(jwtId, jwtToken.Id);
    }

    [Fact]
    public void GenerateGuestToken_SubClaimIsGuestUniqueId_NotSessionId()
    {
        var guestUuid = Guid.NewGuid().ToString("N");
        var (token, _) = _jwtService.GenerateGuestToken(guestUuid, 50, "Khách A");

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(guestUuid, jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("50", jwtToken.Claims.First(c => c.Type == "session_id").Value);
        Assert.Equal("GUEST", jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateGuestToken_TwoGuestsSameSession_HaveDifferentSubClaims()
    {
        var (token1, _) = _jwtService.GenerateGuestToken(Guid.NewGuid().ToString("N"), 50, "Khách A");
        var (token2, _) = _jwtService.GenerateGuestToken(Guid.NewGuid().ToString("N"), 50, "Khách B");

        var handler = new JwtSecurityTokenHandler();
        var sub1 = handler.ReadJwtToken(token1).Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var sub2 = handler.ReadJwtToken(token2).Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
        var sessionId1 = handler.ReadJwtToken(token1).Claims.First(c => c.Type == "session_id").Value;
        var sessionId2 = handler.ReadJwtToken(token2).Claims.First(c => c.Type == "session_id").Value;

        // FIXED: 2 GUEST cùng bàn (cùng session_id) nhưng có sub khác nhau -> phân biệt được danh tính.
        Assert.NotEqual(sub1, sub2);
        Assert.Equal(sessionId1, sessionId2);
    }

    // BUG FIX VERIFIED (regression phát sinh từ việc đổi sub sang UUID, nay đã vá):
    // sub (NameIdentifier) của GUEST là UUID dạng "N", KHÔNG parse được sang int — đây là thiết kế
    // có chủ đích để phân biệt nhiều GUEST cùng bàn. Trước đây AuthController.GetCurrentUser() và
    // Logout() (cả SmartDine.API và SmartDine.Identity.API) đều int.Parse(sub) vô điều kiện nên
    // throw FormatException cho mọi GUEST. Đã fix: cả 2 controller giờ kiểm tra role == "GUEST"
    // trước, và lấy id thực tế từ custom claim "session_id" (JwtClaimTypes.SessionId) thay vì sub —
    // giống cách DiningSessionsController.ExtractIdentity() vốn đã làm đúng từ đầu.
    [Fact]
    public void GenerateGuestToken_SubClaim_IsNotParsableAsInt_ControllersMustUseSessionIdClaimInstead()
    {
        var guestUuid = Guid.NewGuid().ToString("N");
        var (token, _) = _jwtService.GenerateGuestToken(guestUuid, 50, "Khách A");

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var sub = jwtToken.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;

        Assert.False(int.TryParse(sub, out _),
            "sub là UUID, không phải số — AuthController.GetCurrentUser()/Logout() phải đọc claim \"session_id\" cho GUEST thay vì int.Parse(sub)");
    }
}
