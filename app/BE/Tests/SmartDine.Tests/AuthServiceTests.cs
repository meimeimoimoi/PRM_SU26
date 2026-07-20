using Moq;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Application.Services;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
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
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly Mock<IDiningSessionRepository> _sessionRepoMock;
    private readonly Mock<IRepository<SessionParticipant>> _participantRepoMock;
    private readonly Mock<ISettingsRepository> _settingsRepoMock;
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
        _tableRepoMock = new Mock<ITableRepository>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _participantRepoMock = new Mock<IRepository<SessionParticipant>>();
        _settingsRepoMock = new Mock<ISettingsRepository>();

        _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _uowMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
        _uowMock.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepoMock.Object);
        _uowMock.Setup(u => u.PasswordResetTokens).Returns(_passwordResetTokenRepoMock.Object);
        _uowMock.Setup(u => u.Tables).Returns(_tableRepoMock.Object);
        _uowMock.Setup(u => u.DiningSessions).Returns(_sessionRepoMock.Object);
        _uowMock.Setup(u => u.SessionParticipants).Returns(_participantRepoMock.Object);
        _uowMock.Setup(u => u.Settings).Returns(_settingsRepoMock.Object);
        _settingsRepoMock.Setup(r => r.GetSingletonAsync()).ReturnsAsync(new RestaurantSettings
        {
            TaxRate = 8.00m,
            ServiceChargeRate = 5.00m
        });
        _participantRepoMock.Setup(r => r.AddAsync(It.IsAny<SessionParticipant>()))
            .ReturnsAsync((SessionParticipant p) => p);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _authService = new AuthService(_uowMock.Object, _jwtMock.Object, _passwordHasherMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════
    // LOGIN
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Login_ValidUserCredentials_ReturnsTokenResponse()
    {
        var user = new User { Id = 1, Email = "staff@test.com", FullName = "Staff", PasswordHash = "hashed", Role = UserRole.STAFF, IsActive = true };
        _userRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("pass123", "hashed")).Returns(true);
        SetupTokenGeneration(1, "staff@test.com", "Staff", "STAFF");

        var result = await _authService.LoginAsync(new LoginRequest { Email = "staff@test.com", Password = "pass123" });

        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
        Assert.Equal("STAFF", result.User.Role);
    }

    [Fact]
    public async Task Login_ValidCustomerCredentials_ReturnsTokenResponse()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync((User?)null);
        var customer = new Customer { Id = 10, Email = "cust@test.com", FullName = "Customer", PasswordHash = "hashed", Phone = "09000" };
        _customerRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync(customer);
        _passwordHasherMock.Setup(p => p.VerifyPassword("pass123", "hashed")).Returns(true);
        SetupTokenGeneration(10, "cust@test.com", "Customer", "CUSTOMER");

        var result = await _authService.LoginAsync(new LoginRequest { Email = "cust@test.com", Password = "pass123" });

        Assert.Equal("CUSTOMER", result.User.Role);
        Assert.Equal(0, result.SessionId);
    }

    [Fact]
    public async Task Login_CustomerWithTableNumber_CreatesDiningSession()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync((User?)null);
        var customer = new Customer
        {
            Id = 10,
            Email = "cust@test.com",
            FullName = "Customer",
            PasswordHash = "hashed",
            Phone = "09000",
            MembershipLevel = LoyaltyTier.BRONZE
        };
        _customerRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync(customer);
        _passwordHasherMock.Setup(p => p.VerifyPassword("pass123", "hashed")).Returns(true);
        SetupTokenGeneration(10, "cust@test.com", "Customer", "CUSTOMER");

        var table = new Table { Id = 5, TableNumber = 3, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByTableNumberAsync(3)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(5)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => { s.Id = 77; return s; });

        var result = await _authService.LoginAsync(new LoginRequest
        {
            Email = "cust@test.com",
            Password = "pass123",
            TableNumber = 3
        });

        Assert.Equal("CUSTOMER", result.User.Role);
        Assert.Equal(77, result.SessionId);
        Assert.Equal(5, result.TableId);
        Assert.Equal(3, result.TableNumber);
        Assert.Equal(TableStatus.OCCUPIED, table.Status);
        _sessionRepoMock.Verify(r => r.AddAsync(It.IsAny<DiningSession>()), Times.Once);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsBusinessRuleViolation()
    {
        var user = new User { Id = 1, Email = "staff@test.com", PasswordHash = "hashed", IsActive = true };
        _userRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("wrong", "hashed")).Returns(false);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.LoginAsync(new LoginRequest { Email = "staff@test.com", Password = "wrong" }));
    }

    [Fact]
    public async Task Login_NonexistentEmail_ThrowsBusinessRuleViolation()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("nobody@test.com")).ReturnsAsync((User?)null);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("nobody@test.com")).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.LoginAsync(new LoginRequest { Email = "nobody@test.com", Password = "pass" }));
    }

    [Fact]
    public async Task Login_InactiveUser_FallsToCustomerLookup()
    {
        var user = new User { Id = 1, Email = "staff@test.com", PasswordHash = "hashed", IsActive = false };
        _userRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync(user);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.LoginAsync(new LoginRequest { Email = "staff@test.com", Password = "pass" }));
    }

    [Fact]
    public async Task Login_CustomerWithNullPasswordHash_ThrowsBusinessRuleViolation()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("guest@test.com")).ReturnsAsync((User?)null);
        var customer = new Customer { Id = 10, Email = "guest@test.com", PasswordHash = null };
        _customerRepoMock.Setup(r => r.GetByEmailAsync("guest@test.com")).ReturnsAsync(customer);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.LoginAsync(new LoginRequest { Email = "guest@test.com", Password = "pass" }));
    }

    [Fact]
    public async Task Login_CreatesRefreshTokenInDb()
    {
        var user = new User { Id = 1, Email = "staff@test.com", FullName = "Staff", PasswordHash = "h", Role = UserRole.MANAGER, IsActive = true };
        _userRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.VerifyPassword("pass", "h")).Returns(true);
        SetupTokenGeneration(1, "staff@test.com", "Staff", "MANAGER");

        await _authService.LoginAsync(new LoginRequest { Email = "staff@test.com", Password = "pass" });

        _refreshTokenRepoMock.Verify(r => r.AddAsync(It.Is<RefreshToken>(
            rt => rt.UserId == 1 && rt.UserType == UserType.USER)), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════
    // REGISTER
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Register_NewEmail_CreatesCustomerAndReturnsTokens()
    {
        _userRepoMock.Setup(r => r.ExistsAsync("new@test.com")).ReturnsAsync(false);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((Customer?)null);
        _customerRepoMock.Setup(r => r.GetByPhoneAsync("09001")).ReturnsAsync((Customer?)null);
        _passwordHasherMock.Setup(p => p.HashPassword("Password123!")).Returns("hashed");
        _customerRepoMock.Setup(r => r.AddAsync(It.IsAny<Customer>())).ReturnsAsync((Customer c) => c);
        SetupTokenGeneration(0, "new@test.com", "New User", "CUSTOMER");

        var result = await _authService.RegisterAsync(new RegisterRequest
        {
            FullName = "New User", Email = "new@test.com",
            Password = "Password123!", PhoneNumber = "09001"
        });

        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("CUSTOMER", result.User.Role);
        _customerRepoMock.Verify(r => r.AddAsync(It.Is<Customer>(
            c => c.MembershipLevel == LoyaltyTier.BRONZE && c.LoyaltyPoints == 0)), Times.Once);
    }

    [Fact]
    public async Task Register_ExistingEmailInUsers_ThrowsBusinessRuleViolation()
    {
        _userRepoMock.Setup(r => r.ExistsAsync("exists@test.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RegisterAsync(new RegisterRequest
            { FullName = "T", Email = "exists@test.com", Password = "P123!", PhoneNumber = "09001" }));
    }

    [Fact]
    public async Task Register_ExistingEmailInCustomers_ThrowsBusinessRuleViolation()
    {
        _userRepoMock.Setup(r => r.ExistsAsync("cust@test.com")).ReturnsAsync(false);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync(new Customer { Email = "cust@test.com" });

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RegisterAsync(new RegisterRequest
            { FullName = "T", Email = "cust@test.com", Password = "P123!", PhoneNumber = "09002" }));
    }

    [Fact]
    public async Task Register_ExistingPhone_ThrowsBusinessRuleViolation()
    {
        _userRepoMock.Setup(r => r.ExistsAsync("new@test.com")).ReturnsAsync(false);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((Customer?)null);
        _customerRepoMock.Setup(r => r.GetByPhoneAsync("09001")).ReturnsAsync(new Customer { Phone = "09001" });

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RegisterAsync(new RegisterRequest
            { FullName = "T", Email = "new@test.com", Password = "P123!", PhoneNumber = "09001" }));
    }

    [Fact]
    public async Task Register_NullPhone_SkipsPhoneCheck()
    {
        _userRepoMock.Setup(r => r.ExistsAsync("new@test.com")).ReturnsAsync(false);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("new@test.com")).ReturnsAsync((Customer?)null);
        _customerRepoMock.Setup(r => r.AddAsync(It.IsAny<Customer>())).ReturnsAsync((Customer c) => c);
        _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("h");
        SetupTokenGeneration(0, "new@test.com", "T", "CUSTOMER");

        await _authService.RegisterAsync(new RegisterRequest
        { FullName = "T", Email = "new@test.com", Password = "P123!" });

        _customerRepoMock.Verify(r => r.GetByPhoneAsync(It.IsAny<string>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════
    // REFRESH TOKEN
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task RefreshToken_ValidTokens_ReturnsNewTokenPair()
    {
        var claims = CreateClaimsPrincipal("jwt_id_1", "1", "staff@test.com", "Staff", "STAFF");
        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("old_access")).Returns(claims);
        var storedToken = new RefreshToken
        {
            Token = "old_refresh", JwtId = "jwt_id_1", UserId = 1,
            UserType = UserType.USER, ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("old_refresh")).ReturnsAsync(storedToken);
        _jwtMock.Setup(j => j.GenerateAccessToken(1, "staff@test.com", "Staff", "STAFF")).Returns(("new_access", "new_jti"));
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("new_refresh");
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => rt);

        var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest
        { AccessToken = "old_access", RefreshToken = "old_refresh" });

        Assert.Equal("new_access", result.AccessToken);
        Assert.Equal("new_refresh", result.RefreshToken);
        Assert.True(storedToken.IsRevoked);
        Assert.Equal("new_refresh", storedToken.ReplacedByToken);
    }

    [Fact]
    public async Task RefreshToken_InvalidAccessToken_ThrowsException()
    {
        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("invalid")).Returns((ClaimsPrincipal?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest
            { AccessToken = "invalid", RefreshToken = "some_refresh" }));
    }

    [Fact]
    public async Task RefreshToken_ExpiredRefreshToken_ThrowsException()
    {
        var claims = CreateClaimsPrincipal("jwt_id_1", "1", "test@test.com", "Test", "STAFF");
        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("access")).Returns(claims);
        var expired = new RefreshToken { Token = "expired_refresh", JwtId = "jwt_id_1", ExpiresAt = DateTime.UtcNow.AddDays(-1) };
        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("expired_refresh")).ReturnsAsync(expired);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest
            { AccessToken = "access", RefreshToken = "expired_refresh" }));
    }

    [Fact]
    public async Task RefreshToken_JwtIdMismatch_ThrowsException()
    {
        var claims = CreateClaimsPrincipal("jwt_id_1", "1", "t@t.com", "T", "STAFF");
        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("access")).Returns(claims);
        var token = new RefreshToken { Token = "refresh", JwtId = "DIFFERENT_JTI", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("refresh")).ReturnsAsync(token);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest
            { AccessToken = "access", RefreshToken = "refresh" }));
    }

    // BUG FIX VERIFICATION: revoked refresh token should be rejected
    [Fact]
    public async Task RefreshToken_RevokedToken_ThrowsException()
    {
        var claims = CreateClaimsPrincipal("jwt_id_1", "1", "t@t.com", "T", "STAFF");
        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("access")).Returns(claims);
        var revoked = new RefreshToken
        {
            Token = "revoked_refresh", JwtId = "jwt_id_1",
            ExpiresAt = DateTime.UtcNow.AddDays(1), IsRevoked = true
        };
        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("revoked_refresh")).ReturnsAsync(revoked);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest
            { AccessToken = "access", RefreshToken = "revoked_refresh" }));
    }

    [Fact]
    public async Task RefreshToken_NotFoundInDb_ThrowsException()
    {
        var claims = CreateClaimsPrincipal("jti", "1", "t@t.com", "T", "STAFF");
        _jwtMock.Setup(j => j.GetPrincipalFromExpiredToken("access")).Returns(claims);
        _refreshTokenRepoMock.Setup(r => r.GetByTokenAsync("nonexistent")).ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest
            { AccessToken = "access", RefreshToken = "nonexistent" }));
    }

    // ═══════════════════════════════════════════════════════════════
    // FORGOT PASSWORD
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ForgotPassword_ExistingUser_ReturnsResetToken()
    {
        var user = new User { Id = 1, Email = "staff@test.com" };
        _userRepoMock.Setup(r => r.GetByEmailAsync("staff@test.com")).ReturnsAsync(user);
        _jwtMock.Setup(j => j.GeneratePasswordResetToken(1, "staff@test.com", "STAFF")).Returns("reset_token_123");
        _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>())).ReturnsAsync((PasswordResetToken t) => t);

        var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "staff@test.com" });

        Assert.Equal("reset_token_123", result.ResetToken);
        _passwordResetTokenRepoMock.Verify(r => r.InvalidateAllByUserAsync(1, UserType.USER), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ExistingCustomer_ReturnsResetToken()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync((User?)null);
        var customer = new Customer { Id = 5, Email = "cust@test.com" };
        _customerRepoMock.Setup(r => r.GetByEmailAsync("cust@test.com")).ReturnsAsync(customer);
        _jwtMock.Setup(j => j.GeneratePasswordResetToken(5, "cust@test.com", "CUSTOMER")).Returns("reset_cust");
        _passwordResetTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>())).ReturnsAsync((PasswordResetToken t) => t);

        var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "cust@test.com" });

        Assert.Equal("reset_cust", result.ResetToken);
        _passwordResetTokenRepoMock.Verify(r => r.InvalidateAllByUserAsync(5, UserType.CUSTOMER), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_NonexistentEmail_ReturnsGenericMessage_NoToken()
    {
        _userRepoMock.Setup(r => r.GetByEmailAsync("nobody@test.com")).ReturnsAsync((User?)null);
        _customerRepoMock.Setup(r => r.GetByEmailAsync("nobody@test.com")).ReturnsAsync((Customer?)null);

        var result = await _authService.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nobody@test.com" });

        Assert.NotNull(result.Message);
        Assert.Null(result.ResetToken);
    }

    // ═══════════════════════════════════════════════════════════════
    // RESET PASSWORD
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResetPassword_ValidToken_UpdatesPassword()
    {
        var tokenEntity = new PasswordResetToken
        {
            Token = "valid_token", UserId = 1, UserType = UserType.USER,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsUsed = false
        };
        _passwordResetTokenRepoMock.Setup(r => r.GetByTokenAsync("valid_token")).ReturnsAsync(tokenEntity);
        var user = new User { Id = 1, Email = "staff@test.com", PasswordHash = "old_hash" };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.HashPassword("NewPass123!")).Returns("new_hash");

        await _authService.ResetPasswordAsync(new ResetPasswordRequest
        { Token = "valid_token", NewPassword = "NewPass123!", ConfirmPassword = "NewPass123!" });

        Assert.Equal("new_hash", user.PasswordHash);
        Assert.True(tokenEntity.IsUsed);
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(1, UserType.USER), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_MismatchedPasswords_ThrowsException()
    {
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.ResetPasswordAsync(new ResetPasswordRequest
            { Token = "token", NewPassword = "Pass1", ConfirmPassword = "Pass2" }));
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ThrowsException()
    {
        _passwordResetTokenRepoMock.Setup(r => r.GetByTokenAsync("invalid")).ReturnsAsync((PasswordResetToken?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.ResetPasswordAsync(new ResetPasswordRequest
            { Token = "invalid", NewPassword = "NewPass123!", ConfirmPassword = "NewPass123!" }));
    }

    // BUG FIX VERIFICATION: used token should be rejected
    [Fact]
    public async Task ResetPassword_AlreadyUsedToken_ThrowsException()
    {
        var tokenEntity = new PasswordResetToken
        {
            Token = "used_token", UserId = 1, UserType = UserType.USER,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsUsed = true
        };
        _passwordResetTokenRepoMock.Setup(r => r.GetByTokenAsync("used_token")).ReturnsAsync(tokenEntity);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.ResetPasswordAsync(new ResetPasswordRequest
            { Token = "used_token", NewPassword = "P123!", ConfirmPassword = "P123!" }));
    }

    // BUG FIX VERIFICATION: expired token should be rejected
    [Fact]
    public async Task ResetPassword_ExpiredToken_ThrowsException()
    {
        var tokenEntity = new PasswordResetToken
        {
            Token = "expired_token", UserId = 1, UserType = UserType.USER,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), IsUsed = false
        };
        _passwordResetTokenRepoMock.Setup(r => r.GetByTokenAsync("expired_token")).ReturnsAsync(tokenEntity);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _authService.ResetPasswordAsync(new ResetPasswordRequest
            { Token = "expired_token", NewPassword = "P123!", ConfirmPassword = "P123!" }));
    }

    [Fact]
    public async Task ResetPassword_CustomerToken_UpdatesCustomerPassword()
    {
        var tokenEntity = new PasswordResetToken
        {
            Token = "cust_token", UserId = 5, UserType = UserType.CUSTOMER,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10), IsUsed = false
        };
        _passwordResetTokenRepoMock.Setup(r => r.GetByTokenAsync("cust_token")).ReturnsAsync(tokenEntity);
        var customer = new Customer { Id = 5, PasswordHash = "old" };
        _customerRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(customer);
        _passwordHasherMock.Setup(p => p.HashPassword("New123!")).Returns("new");

        await _authService.ResetPasswordAsync(new ResetPasswordRequest
        { Token = "cust_token", NewPassword = "New123!", ConfirmPassword = "New123!" });

        Assert.Equal("new", customer.PasswordHash);
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(5, UserType.CUSTOMER), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════
    // GET CURRENT USER
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCurrentUser_StaffRole_ReturnsUserInfo()
    {
        var user = new User { Id = 1, FullName = "Staff", Email = "staff@test.com", Role = UserRole.STAFF };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await _authService.GetCurrentUserAsync(1, "STAFF");

        Assert.Equal("Staff", result.FullName);
        Assert.Equal("STAFF", result.Role);
    }

    [Fact]
    public async Task GetCurrentUser_CustomerRole_ReturnsCustomerInfo()
    {
        var customer = new Customer { Id = 10, FullName = "Cust", Email = "cust@test.com" };
        _customerRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(customer);

        var result = await _authService.GetCurrentUserAsync(10, "CUSTOMER");

        Assert.Equal("Cust", result.FullName);
        Assert.Equal("CUSTOMER", result.Role);
    }

    // BUG FIX VERIFICATION: GUEST role now handled without crashing
    [Fact]
    public async Task GetCurrentUser_GuestRole_ReturnsGuestInfo()
    {
        var result = await _authService.GetCurrentUserAsync(77, "GUEST");

        Assert.Equal("Guest", result.FullName);
        Assert.Equal("GUEST", result.Role);
        Assert.Equal(77, result.Id);
    }

    [Fact]
    public async Task GetCurrentUser_InvalidId_ThrowsEntityNotFound()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _authService.GetCurrentUserAsync(999, "STAFF"));
    }

    [Fact]
    public async Task GetCurrentUser_CustomerNotFound_ThrowsEntityNotFound()
    {
        _customerRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _authService.GetCurrentUserAsync(999, "CUSTOMER"));
    }

    // ═══════════════════════════════════════════════════════════════
    // LOGOUT
    // ═══════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(UserType.USER)]
    [InlineData(UserType.CUSTOMER)]
    [InlineData(UserType.GUEST)]
    public async Task Logout_AllUserTypes_RevokesAndReturnsSuccess(UserType userType)
    {
        var result = await _authService.LogoutAsync(1, userType);

        Assert.Contains("thành công", result.Message);
        _refreshTokenRepoMock.Verify(r => r.RevokeAllByUserAsync(1, userType), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════
    // GUEST LOGIN
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task LoginGuest_AvailableTable_CreatesSessionAndReturnsToken()
    {
        var table = new Table { Id = 5, TableNumber = 12, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByTableNumberAsync(12)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(5)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => { s.Id = 120; return s; });
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), 120, "Anh Hoàng"))
            .Returns(("guest_token", "jti"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        { TableId = 12, GuestName = "Anh Hoàng", GuestPhone = "0912345678" });

        Assert.Equal("guest_token", result.Token);
        Assert.Equal(120, result.SessionId);
        Assert.Equal(12, result.TableNumber);
        Assert.Equal("GUEST", result.Role);
        Assert.Equal(TableStatus.OCCUPIED, table.Status);
    }

    [Fact]
    public async Task LoginGuest_ExistingSession_ReusesWithoutCreatingNew()
    {
        var table = new Table { Id = 5, TableNumber = 12, Status = TableStatus.OCCUPIED };
        var session = new DiningSession { Id = 99, TableId = 5, Status = DiningSessionStatus.ACTIVE };
        _tableRepoMock.Setup(r => r.GetByTableNumberAsync(12)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(5)).ReturnsAsync(session);
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), 99, It.IsAny<string>())).Returns(("t", "j"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        { TableId = 12, GuestName = "Khách 2" });

        Assert.Equal(99, result.SessionId);
        _sessionRepoMock.Verify(r => r.AddAsync(It.IsAny<DiningSession>()), Times.Never);
        // SaveChanges vẫn được gọi 1 lần để lưu SessionParticipant của khách mới tham gia
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginGuest_TableNotFound_ThrowsEntityNotFound()
    {
        _tableRepoMock.Setup(r => r.GetByTableNumberAsync(999)).ReturnsAsync((Table?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 999, GuestName = "Khách" }));
    }

    [Fact]
    public async Task LoginGuest_NullGuestName_UsesGuestDefault()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByTableNumberAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>())).ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), "Guest")).Returns(("t", "j"));

        await _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 1 });

        _jwtMock.Verify(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), "Guest"), Times.Once);
    }

    [Fact]
    public async Task LoginGuest_DoesNotCreateRefreshToken()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByTableNumberAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>())).ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(("t", "j"));

        await _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 1, GuestName = "K" });

        _refreshTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private void SetupTokenGeneration(int userId, string email, string fullName, string role)
    {
        _jwtMock.Setup(j => j.GenerateAccessToken(userId, email, fullName, role)).Returns(("access_token", "jwt_id"));
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh_token");
        _refreshTokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).ReturnsAsync((RefreshToken rt) => rt);
    }

    private static ClaimsPrincipal CreateClaimsPrincipal(string jti, string userId, string email, string name, string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role),
        }));
    }
}
