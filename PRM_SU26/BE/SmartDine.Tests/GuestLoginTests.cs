using Moq;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Application.Services;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Tests;

public class GuestLoginTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IJwtTokenService> _jwtMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly Mock<IDiningSessionRepository> _sessionRepoMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepoMock;
    private readonly AuthService _authService;

    public GuestLoginTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _jwtMock = new Mock<IJwtTokenService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tableRepoMock = new Mock<ITableRepository>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _passwordResetTokenRepoMock = new Mock<IPasswordResetTokenRepository>();

        _uowMock.Setup(u => u.Tables).Returns(_tableRepoMock.Object);
        _uowMock.Setup(u => u.DiningSessions).Returns(_sessionRepoMock.Object);
        _uowMock.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepoMock.Object);
        _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _uowMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
        _uowMock.Setup(u => u.PasswordResetTokens).Returns(_passwordResetTokenRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _authService = new AuthService(_uowMock.Object, _jwtMock.Object, _passwordHasherMock.Object);
    }

    // ===== HAPPY PATH =====

    [Fact]
    public async Task LoginGuest_NewSessionAtAvailableTable_CreatesSessionAndReturnsToken()
    {
        var table = new Table { Id = 5, TableNumber = 12, Status = TableStatus.AVAILABLE, Capacity = 4 };
        _tableRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(5)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => { s.Id = 120; return s; });
        _jwtMock.Setup(j => j.GenerateAccessToken(120, "", "Anh Hoàng", "GUEST"))
            .Returns(("guest_token_abc", "jti_guest"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 5,
            GuestName = "Anh Hoàng",
            GuestPhone = "0912345678"
        });

        Assert.Equal("guest_token_abc", result.Token);
        Assert.Equal(120, result.SessionId);
        Assert.Equal(5, result.TableId);
        Assert.Equal(12, result.TableNumber);
        Assert.Equal("GUEST", result.Role);
    }

    [Fact]
    public async Task LoginGuest_NewSession_SetsTableStatusToOccupied()
    {
        var table = new Table { Id = 3, TableNumber = 7, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(3)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", It.IsAny<string>(), "GUEST"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 3,
            GuestName = "Khách"
        });

        Assert.Equal(TableStatus.OCCUPIED, table.Status);
    }

    [Fact]
    public async Task LoginGuest_NewSession_PersistsGuestInfoInSession()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);

        DiningSession? capturedSession = null;
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .Callback<DiningSession>(s => capturedSession = s)
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", It.IsAny<string>(), "GUEST"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 1,
            GuestName = "Nguyễn Văn A",
            GuestPhone = "0901234567"
        });

        Assert.NotNull(capturedSession);
        Assert.Equal("Nguyễn Văn A", capturedSession!.GuestName);
        Assert.Equal("0901234567", capturedSession.GuestPhone);
        Assert.Equal(DiningSessionStatus.ACTIVE, capturedSession.Status);
        Assert.Equal(1, capturedSession.TableId);
    }

    [Fact]
    public async Task LoginGuest_NewSession_CallsSaveChanges()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", It.IsAny<string>(), "GUEST"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 1, GuestName = "Test" });

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sessionRepoMock.Verify(r => r.AddAsync(It.IsAny<DiningSession>()), Times.Once);
    }

    // ===== EXISTING SESSION - Khách thứ 2 quét QR cùng bàn =====

    [Fact]
    public async Task LoginGuest_ExistingActiveSession_ReusesSessionWithoutCreatingNew()
    {
        var table = new Table { Id = 5, TableNumber = 12, Status = TableStatus.OCCUPIED };
        var existingSession = new DiningSession
        {
            Id = 99,
            TableId = 5,
            GuestName = "Khách trước",
            GuestPhone = "0900000000",
            Status = DiningSessionStatus.ACTIVE
        };

        _tableRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(5)).ReturnsAsync(existingSession);
        _jwtMock.Setup(j => j.GenerateAccessToken(99, "", "Khách mới", "GUEST"))
            .Returns(("guest_token_2", "jti_2"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 5,
            GuestName = "Khách mới",
            GuestPhone = "0911111111"
        });

        Assert.Equal(99, result.SessionId);
        Assert.Equal("guest_token_2", result.Token);
        _sessionRepoMock.Verify(r => r.AddAsync(It.IsAny<DiningSession>()), Times.Never);
    }

    [Fact]
    public async Task LoginGuest_ExistingActiveSession_DoesNotCallSaveChanges()
    {
        var table = new Table { Id = 5, TableNumber = 12, Status = TableStatus.OCCUPIED };
        var existingSession = new DiningSession { Id = 99, TableId = 5, Status = DiningSessionStatus.ACTIVE };

        _tableRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(5)).ReturnsAsync(existingSession);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", It.IsAny<string>(), "GUEST"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 5, GuestName = "Test" });

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ===== ERROR CASES =====

    [Fact]
    public async Task LoginGuest_TableNotFound_ThrowsEntityNotFoundException()
    {
        _tableRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Table?)null);

        var ex = await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _authService.LoginGuestAsync(new GuestLoginRequest
            {
                TableId = 999,
                GuestName = "Khách"
            }));

        Assert.Contains("Table", ex.Message);
    }

    [Fact]
    public async Task LoginGuest_SoftDeletedTable_ThrowsEntityNotFoundException()
    {
        _tableRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Table?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 10 }));
    }

    // ===== EDGE CASES - Khách ẩn danh =====

    [Fact]
    public async Task LoginGuest_NullGuestName_UsesGuestAsDefaultInToken()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);

        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", "Guest", "GUEST"))
            .Returns(("anonymous_token", "jti"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 1,
            GuestName = null,
            GuestPhone = null
        });

        Assert.Equal("anonymous_token", result.Token);
        _jwtMock.Verify(j => j.GenerateAccessToken(It.IsAny<int>(), "", "Guest", "GUEST"), Times.Once);
    }

    [Fact]
    public async Task LoginGuest_NullGuestName_StoresNullInSession()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);

        DiningSession? captured = null;
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .Callback<DiningSession>(s => captured = s)
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", "Guest", "GUEST"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 1 });

        Assert.Null(captured!.GuestName);
        Assert.Null(captured.GuestPhone);
    }

    [Fact]
    public async Task LoginGuest_EmptyStringGuestName_UsesEmptyStringNotDefault()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);

        // BUG DETECTION: empty string "" is not null, so ?? won't trigger.
        // Token gets generated with "" as name instead of "Guest".
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", "", "GUEST"))
            .Returns(("empty_name_token", "jti"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 1,
            GuestName = "",
            GuestPhone = ""
        });

        // Verifies that empty string passes through — potential issue for JWT claim readability
        _jwtMock.Verify(j => j.GenerateAccessToken(It.IsAny<int>(), "", "", "GUEST"), Times.Once);
    }

    // ===== CONCURRENT SCENARIO - Nhiều khách quét cùng lúc =====

    [Fact]
    public async Task LoginGuest_MultipleGuestsAtSameTable_AllGetSameSessionId()
    {
        var table = new Table { Id = 5, TableNumber = 12, Status = TableStatus.AVAILABLE };
        var session = new DiningSession { Id = 200, TableId = 5, Status = DiningSessionStatus.ACTIVE, GuestName = "Khách 1" };

        _tableRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(table);

        // First call: no session exists
        _sessionRepoMock.SetupSequence(r => r.GetActiveByTableIdAsync(5))
            .ReturnsAsync((DiningSession?)null)
            .ReturnsAsync(session);

        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => { s.Id = 200; return s; });

        _jwtMock.Setup(j => j.GenerateAccessToken(200, "", It.IsAny<string>(), "GUEST"))
            .Returns(("token_1", "jti_1"));

        var result1 = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 5, GuestName = "Khách 1"
        });

        _jwtMock.Setup(j => j.GenerateAccessToken(200, "", "Khách 2", "GUEST"))
            .Returns(("token_2", "jti_2"));

        var result2 = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 5, GuestName = "Khách 2"
        });

        Assert.Equal(200, result1.SessionId);
        Assert.Equal(200, result2.SessionId);
        Assert.NotEqual(result1.Token, result2.Token);
    }

    // ===== RESPONSE FORMAT VALIDATION =====

    [Fact]
    public async Task LoginGuest_ResponseContainsCorrectTableNumber()
    {
        var table = new Table { Id = 3, TableNumber = 42, Status = TableStatus.AVAILABLE, Capacity = 6 };
        _tableRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(3)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", It.IsAny<string>(), "GUEST"))
            .Returns(("token", "jti"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 3, GuestName = "Test"
        });

        Assert.Equal(42, result.TableNumber);
        Assert.Equal(3, result.TableId);
    }

    [Fact]
    public async Task LoginGuest_RoleAlwaysGuest_RegardlessOfInput()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", It.IsAny<string>(), "GUEST"))
            .Returns(("token", "jti"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 1, GuestName = "Hacker"
        });

        Assert.Equal("GUEST", result.Role);
    }

    // ===== BUG DETECTION: Token không có refresh token =====

    [Fact]
    public async Task LoginGuest_DoesNotCreateRefreshToken_GuestCannotRefreshSession()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", It.IsAny<string>(), "GUEST"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 1, GuestName = "Khách"
        });

        // BUG: LoginGuestAsync does NOT create a RefreshToken.
        // Guests cannot refresh their session after the 60-min access token expires.
        // They must re-scan the QR code. This may be intentional but differs from
        // the normal login flow (LoginAsync) which always creates a refresh token.
        _refreshTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
        _jwtMock.Verify(j => j.GenerateRefreshToken(), Times.Never);
    }

    // ===== BUG DETECTION: Email rỗng trong JWT claim =====

    [Fact]
    public async Task LoginGuest_GeneratesTokenWithEmptyEmail()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<int>(), "", It.IsAny<string>(), "GUEST"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 1, GuestName = "Khách"
        });

        // BUG: Token is generated with empty string email ("").
        // The /api/v1/auth/me endpoint tries to look up user by role,
        // but GUEST role is not handled in GetCurrentUserAsync — it falls
        // into the else branch (User lookup) and will throw EntityNotFoundException
        // because session.Id is used as userId, which doesn't exist in the Users table.
        _jwtMock.Verify(j => j.GenerateAccessToken(It.IsAny<int>(), "", It.IsAny<string>(), "GUEST"), Times.Once);
    }

    // ===== BUG DETECTION: SessionId trong token = session ID, không phải user ID =====

    [Fact]
    public async Task LoginGuest_UsesSessionIdAsIdentifier_NotUserId()
    {
        var table = new Table { Id = 5, TableNumber = 12, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(5)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => { s.Id = 77; return s; });

        _jwtMock.Setup(j => j.GenerateAccessToken(77, "", "Khách", "GUEST"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 5, GuestName = "Khách"
        });

        // The token's NameIdentifier claim = session.Id (77), not a user/customer ID.
        // This means Logout endpoint will call RevokeAllByUserAsync(77, "GUEST")
        // which won't find any refresh tokens (since none were created).
        // Also, GetCurrentUserAsync(77, "GUEST") will fail because GUEST role
        // is not handled — it falls into the User lookup path.
        _jwtMock.Verify(j => j.GenerateAccessToken(77, "", "Khách", "GUEST"), Times.Once);
    }
}
