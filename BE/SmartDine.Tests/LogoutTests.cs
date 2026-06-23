using Moq;
using SmartDine.Application.Constants;
using SmartDine.Application.Services;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Tests;

public class LogoutTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IJwtTokenService> _jwtMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepoMock;
    private readonly AuthService _authService;

    public LogoutTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _jwtMock = new Mock<IJwtTokenService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _passwordResetTokenRepoMock = new Mock<IPasswordResetTokenRepository>();

        _uowMock.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepoMock.Object);
        _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _uowMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
        _uowMock.Setup(u => u.PasswordResetTokens).Returns(_passwordResetTokenRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _authService = new AuthService(_uowMock.Object, _jwtMock.Object, _passwordHasherMock.Object);
    }

    // ===== HAPPY PATH =====

    [Fact]
    public async Task Logout_Staff_RevokesAllRefreshTokensAndReturnsMessage()
    {
        var result = await _authService.LogoutAsync(1, "USER");

        Assert.Equal(ValidationMessages.LOGOUT_SUCCESS, result.Message);
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(1, "USER"), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_Customer_RevokesWithCustomerUserType()
    {
        var result = await _authService.LogoutAsync(10, "CUSTOMER");

        Assert.Equal(ValidationMessages.LOGOUT_SUCCESS, result.Message);
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(10, "CUSTOMER"), Times.Once);
    }

    [Fact]
    public async Task Logout_Manager_RevokesWithUserType()
    {
        var result = await _authService.LogoutAsync(2, "USER");

        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(2, "USER"), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== GUEST LOGOUT =====

    [Fact]
    public async Task Logout_Guest_CompletesSuccessfully_EvenWithoutRefreshTokens()
    {
        // Guest login never creates refresh tokens, so RevokeAllByUserAsync
        // will be a no-op but should not throw.
        var result = await _authService.LogoutAsync(99, "GUEST");

        Assert.Equal(ValidationMessages.LOGOUT_SUCCESS, result.Message);
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(99, "GUEST"), Times.Once);
    }

    // ===== EDGE CASES =====

    [Fact]
    public async Task Logout_UserWithNoActiveTokens_StillSucceeds()
    {
        // User already logged out everywhere, or tokens already expired.
        var result = await _authService.LogoutAsync(5, "USER");

        Assert.NotNull(result);
        Assert.Equal(ValidationMessages.LOGOUT_SUCCESS, result.Message);
    }

    [Fact]
    public async Task Logout_AlwaysCallsSaveChanges()
    {
        await _authService.LogoutAsync(1, "USER");
        await _authService.LogoutAsync(2, "CUSTOMER");
        await _authService.LogoutAsync(3, "GUEST");

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Theory]
    [InlineData("USER")]
    [InlineData("CUSTOMER")]
    [InlineData("GUEST")]
    public async Task Logout_AllUserTypes_ReturnsConsistentMessage(string userType)
    {
        var result = await _authService.LogoutAsync(1, userType);

        Assert.Contains("thành công", result.Message);
    }

    // ===== BUG DETECTION: Access token vẫn valid sau logout =====

    [Fact]
    public async Task Logout_DoesNotInvalidateAccessToken_SecurityConcern()
    {
        // After logout, the JWT access token remains valid until it expires (60 min).
        // Without Redis token blacklisting (as spec requires), a stolen token
        // can still be used for up to 60 minutes after logout.
        //
        // The spec says: "Đăng xuất tài khoản và vô hiệu hóa Token hiện tại
        // trong cơ sở dữ liệu lưu trữ đệm (như Redis)."
        //
        // Current implementation only revokes refresh tokens in PostgreSQL,
        // not the access token itself. This is a security gap.

        await _authService.LogoutAsync(1, "USER");

        // Verify only refresh tokens are revoked — access token is NOT blacklisted.
        // There is no call to any cache/Redis service to invalidate the JWT itself.
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(1, "USER"), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== SCENARIO: Login rồi Logout rồi Login lại =====

    [Fact]
    public async Task Logout_ThenLoginAgain_GetsNewTokens()
    {
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync((RefreshToken rt) => rt);

        var user = new User
        {
            Id = 1, Email = "staff@test.com", FullName = "Staff",
            PasswordHash = "hashed", Role = "STAFF", IsActive = true
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("pass", "hashed")).Returns(true);
        _jwtMock.SetupSequence(j => j.GenerateAccessToken(1, "staff@test.com", "Staff", "STAFF"))
            .Returns(("token_before_logout", "jti_1"))
            .Returns(("token_after_logout", "jti_2"));
        _jwtMock.SetupSequence(j => j.GenerateRefreshToken())
            .Returns("refresh_1")
            .Returns("refresh_2");

        var login1 = await _authService.LoginAsync(new Application.DTOs.Auth.LoginRequest
        {
            Email = "staff@test.com", Password = "pass"
        });

        await _authService.LogoutAsync(1, "USER");

        var login2 = await _authService.LoginAsync(new Application.DTOs.Auth.LoginRequest
        {
            Email = "staff@test.com", Password = "pass"
        });

        Assert.NotEqual(login1.AccessToken, login2.AccessToken);
        Assert.NotEqual(login1.RefreshToken, login2.RefreshToken);
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(1, "USER"), Times.Once);
    }

    // ===== SCENARIO: Logout nhiều lần liên tục =====

    [Fact]
    public async Task Logout_CalledMultipleTimes_NoErrorOnSecondCall()
    {
        var result1 = await _authService.LogoutAsync(1, "USER");
        var result2 = await _authService.LogoutAsync(1, "USER");

        Assert.Equal(result1.Message, result2.Message);
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(1, "USER"), Times.Exactly(2));
    }
}
