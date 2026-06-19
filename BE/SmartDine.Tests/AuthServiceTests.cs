using Moq;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Application.Services;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SmartDine.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IJwtTokenService> _jwtMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepoMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _jwtMock = new Mock<IJwtTokenService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _userRepoMock = new Mock<IUserRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _passwordResetTokenRepoMock = new Mock<IPasswordResetTokenRepository>();

        _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _uowMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
        _uowMock.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepoMock.Object);
        _uowMock.Setup(u => u.PasswordResetTokens).Returns(_passwordResetTokenRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _authService = new AuthService(_uowMock.Object, _jwtMock.Object, _passwordHasherMock.Object);
    }

    // ===== LOGIN TESTS =====

    [Fact]
    public async Task Login_WithValidUserCredentials_ReturnsTokenResponse()
    {
        var user = new User { Id = 1, Email = "staff@test.com", FullName = "Staff", PasswordHash = "hashed", Role = "STAFF", IsActive = true };
        _userRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("pass123", "hashed")).Returns(true);
        _jwtMock.Setup(j => j.GenerateAccessToken(1, "staff@test.com", "Staff", "STAFF")).Returns(("access_token", "jwt_id"));
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => rt);

        var result = await _authService.LoginAsync(new LoginRequest { Email = "staff@test.com", Password = "pass123" });

        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
        Assert.Equal("STAFF", result.User.Role);
    }

    [Fact]
    public async Task Login_WithValidCustomerCredentials_ReturnsTokenResponse()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync((User?)null);
        var customer = new Customer { Id = 10, Email = "cust@test.com", FullName = "Customer", PasswordHash = "hashed", Phone = "09000" };
        _customerRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync(customer);
        _passwordHasherMock.Setup(p => p.VerifyPassword("pass123", "hashed")).Returns(true);
        _jwtMock.Setup(j => j.GenerateAccessToken(10, "cust@test.com", "Customer", "CUSTOMER")).Returns(("access_token", "jwt_id"));
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => rt);

        var result = await _authService.LoginAsync(new LoginRequest { Email = "cust@test.com", Password = "pass123" });

        Assert.Equal("CUSTOMER", result.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsBusinessRuleViolation()
    {
        var user = new User { Id = 1, Email = "staff@test.com", PasswordHash = "hashed", IsActive = true };
        _userRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("wrong", "hashed")).Returns(false);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.LoginAsync(new LoginRequest { Email = "staff@test.com", Password = "wrong" }));
    }

    [Fact]
    public async Task Login_WithNonexistentEmail_ThrowsBusinessRuleViolation()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("nobody@test.com")).ReturnsAsync((User?)null);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("nobody@test.com")).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.LoginAsync(new LoginRequest { Email = "nobody@test.com", Password = "pass" }));
    }

    // ===== REGISTER TESTS =====

    [Fact]
    public async Task Register_WithNewEmail_CreatesCustomerAndReturnsTokens()
    {
        _userRepoMock.Setup(r => r.ExistsAsync("new@test.com")).ReturnsAsync(false);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((Customer?)null);
        _customerRepoMock.Setup(r => r.GetByPhoneAsync("09001")).ReturnsAsync((Customer?)null);
        _passwordHasherMock.Setup(p => p.HashPassword("Password123!")).Returns("hashed");
        _customerRepoMock.Setup(r => r.AddAsync(It.IsAny<Customer>())).ReturnsAsync((Customer c) => c);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "new@test.com", "New User", "CUSTOMER")).Returns(("token", "jti"));
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh");
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => rt);

        var result = await _authService.RegisterAsync(new RegisterRequest
        {
            FullName = "New User",
            Email = "new@test.com",
            Password = "Password123!",
            PhoneNumber = "09001"
        });

        Assert.Equal("token", result.AccessToken);
        Assert.Equal("CUSTOMER", result.User.Role);
        _customerRepoMock.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsBusinessRuleViolation()
    {
        _userRepoMock.Setup(r => r.ExistsAsync("exists@test.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RegisterAsync(new RegisterRequest
            {
                FullName = "Test",
                Email = "exists@test.com",
                Password = "Password123!",
                PhoneNumber = "09001"
            }));
    }

    [Fact]
    public async Task Register_WithExistingPhone_ThrowsBusinessRuleViolation()
    {
        _userRepoMock.Setup(r => r.ExistsAsync("new@test.com")).ReturnsAsync(false);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((Customer?)null);
        _customerRepoMock.Setup(r => r.GetByPhoneAsync("09001")).ReturnsAsync(new Customer { Phone = "09001" });

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RegisterAsync(new RegisterRequest
            {
                FullName = "Test",
                Email = "new@test.com",
                Password = "Password123!",
                PhoneNumber = "09001"
            }));
    }

    // ===== REFRESH TOKEN TESTS =====

    [Fact]
    public async Task RefreshToken_WithValidTokens_ReturnsNewTokenPair()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, "jwt_id_1"),
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "staff@test.com"),
            new Claim(ClaimTypes.Name, "Staff"),
            new Claim(ClaimTypes.Role, "STAFF"),
        }));

        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("old_access")).Returns(claims);
        var storedToken = new RefreshToken
        {
            Token = "old_refresh",
            JwtId = "jwt_id_1",
            UserId = 1,
            UserType = "USER",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("old_refresh")).ReturnsAsync(storedToken);
        _jwtMock.Setup(j => j.GenerateAccessToken(1, "staff@test.com", "Staff", "STAFF")).Returns(("new_access", "new_jti"));
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("new_refresh");
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => rt);

        var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest
        {
            AccessToken = "old_access",
            RefreshToken = "old_refresh"
        });

        Assert.Equal("new_access", result.AccessToken);
        Assert.Equal("new_refresh", result.RefreshToken);
        Assert.True(storedToken.IsRevoked);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidAccessToken_ThrowsException()
    {
        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("invalid")).Returns((ClaimsPrincipal?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest
            {
                AccessToken = "invalid",
                RefreshToken = "some_refresh"
            }));
    }

    [Fact]
    public async Task RefreshToken_WithExpiredRefreshToken_ThrowsException()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, "jwt_id_1"),
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "test@test.com"),
            new Claim(ClaimTypes.Name, "Test"),
            new Claim(ClaimTypes.Role, "STAFF"),
        }));

        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("access")).Returns(claims);
        var expired = new RefreshToken
        {
            Token = "expired_refresh",
            JwtId = "jwt_id_1",
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("expired_refresh")).ReturnsAsync(expired);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest
            {
                AccessToken = "access",
                RefreshToken = "expired_refresh"
            }));
    }

    // ===== FORGOT PASSWORD TESTS =====

    [Fact]
    public async Task ForgotPassword_WithExistingUserEmail_ReturnsResetToken()
    {
        var user = new User { Id = 1, Email = "staff@test.com" };
        _userRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync(user);
        _jwtMock.Setup(j => j.GeneratePasswordResetToken()).Returns("reset_token_123");
        _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>())).ReturnsAsync((PasswordResetToken t) => t);

        var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "staff@test.com" });

        Assert.Equal("reset_token_123", result.ResetToken);
        _passwordResetTokenRepoMock.Verify(r => r.InvalidateAllByUserAsync(1, "USER"), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WithExistingCustomerEmail_ReturnsResetToken()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync((User?)null);
        var customer = new Customer { Id = 5, Email = "cust@test.com" };
        _customerRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync(customer);
        _jwtMock.Setup(j => j.GeneratePasswordResetToken()).Returns("reset_cust");
        _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>())).ReturnsAsync((PasswordResetToken t) => t);

        var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "cust@test.com" });

        Assert.Equal("reset_cust", result.ResetToken);
    }

    [Fact]
    public async Task ForgotPassword_WithNonexistentEmail_ReturnsGenericMessage_NoToken()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("nobody@test.com")).ReturnsAsync((User?)null);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("nobody@test.com")).ReturnsAsync((Customer?)null);

        var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nobody@test.com" });

        Assert.NotNull(result.Message);
        Assert.Null(result.ResetToken);
    }

    // ===== RESET PASSWORD TESTS =====

    [Fact]
    public async Task ResetPassword_WithValidToken_UpdatesPassword()
    {
        var tokenEntity = new PasswordResetToken
        {
            Token = "valid_token",
            UserId = 1,
            UserType = "USER",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };
        _passwordResetTokenRepoMock.Setup(r => r.GetByTokenAsync("valid_token")).ReturnsAsync(tokenEntity);
        var user = new User { Id = 1, Email = "staff@test.com", PasswordHash = "old_hash" };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.HashPassword("NewPass123!")).Returns("new_hash");

        await _authService.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "valid_token",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        });

        Assert.Equal("new_hash", user.PasswordHash);
        Assert.True(tokenEntity.IsUsed);
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(1, "USER"), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithMismatchedPasswords_ThrowsException()
    {
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.ResetPasswordAsync(new ResetPasswordRequest
            {
                Token = "token",
                NewPassword = "Pass1",
                ConfirmPassword = "Pass2"
            }));
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ThrowsException()
    {
        _passwordResetTokenRepoMock.Setup(r => r.GetByTokenAsync("invalid")).ReturnsAsync((PasswordResetToken?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.ResetPasswordAsync(new ResetPasswordRequest
            {
                Token = "invalid",
                NewPassword = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            }));
    }

    // ===== GET CURRENT USER TESTS =====

    [Fact]
    public async Task GetCurrentUser_WithValidUserId_ReturnsUserInfo()
    {
        var user = new User { Id = 1, FullName = "Staff", Email = "staff@test.com", Role = "STAFF" };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await _authService.GetCurrentUserAsync(1, "STAFF");

        Assert.Equal("Staff", result.FullName);
        Assert.Equal("STAFF", result.Role);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidCustomerId_ReturnsCustomerInfo()
    {
        var customer = new Customer { Id = 10, FullName = "Cust", Email = "cust@test.com" };
        _customerRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(customer);

        var result = await _authService.GetCurrentUserAsync(10, "CUSTOMER");

        Assert.Equal("Cust", result.FullName);
        Assert.Equal("CUSTOMER", result.Role);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidId_ThrowsEntityNotFound()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _authService.GetCurrentUserAsync(999, "STAFF"));
    }
}
