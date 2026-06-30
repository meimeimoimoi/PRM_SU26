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
    private readonly Mock<IRepository<SessionParticipant>> _participantRepoMock;
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
        _participantRepoMock = new Mock<IRepository<SessionParticipant>>();

        _uowMock.Setup(u => u.Tables).Returns(_tableRepoMock.Object);
        _uowMock.Setup(u => u.DiningSessions).Returns(_sessionRepoMock.Object);
        _uowMock.Setup(u => u.RefreshTokens).Returns(_refreshTokenRepoMock.Object);
        _uowMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _uowMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
        _uowMock.Setup(u => u.PasswordResetTokens).Returns(_passwordResetTokenRepoMock.Object);
        _uowMock.Setup(u => u.SessionParticipants).Returns(_participantRepoMock.Object);
        _participantRepoMock.Setup(r => r.AddAsync(It.IsAny<SessionParticipant>()))
            .ReturnsAsync((SessionParticipant p) => p);
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
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), 120, "Anh Hoàng"))
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
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
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
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
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
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 1, GuestName = "Test" });

        // SaveChanges được gọi 2 lần: 1 lần lưu DiningSession mới, 1 lần lưu SessionParticipant (HOST)
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _sessionRepoMock.Verify(r => r.AddAsync(It.IsAny<DiningSession>()), Times.Once);
        _participantRepoMock.Verify(r => r.AddAsync(It.Is<SessionParticipant>(p => p.Role == ParticipantRole.HOST)), Times.Once);
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
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), 99, "Khách mới"))
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
    public async Task LoginGuest_ExistingActiveSession_StillCallsSaveChangesForParticipant()
    {
        var table = new Table { Id = 5, TableNumber = 12, Status = TableStatus.OCCUPIED };
        var existingSession = new DiningSession { Id = 99, TableId = 5, Status = DiningSessionStatus.ACTIVE };
        // Khách đầu tiên (HOST) đã có mặt từ trước trong session
        existingSession.Participants.Add(new SessionParticipant
        {
            SessionId = 99, GuestSessionId = "first-guest-uuid", Role = ParticipantRole.HOST
        });

        _tableRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(5)).ReturnsAsync(existingSession);
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 5, GuestName = "Test" });

        // Không tạo DiningSession mới, nhưng VẪN gọi SaveChanges 1 lần để lưu SessionParticipant.
        _sessionRepoMock.Verify(r => r.AddAsync(It.IsAny<DiningSession>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Đã có HOST -> khách thứ 2 vào cùng bàn phải là MEMBER, không phải HOST.
        _participantRepoMock.Verify(r => r.AddAsync(It.Is<SessionParticipant>(p => p.Role == ParticipantRole.MEMBER)), Times.Once);
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

        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), "Guest"))
            .Returns(("anonymous_token", "jti"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 1,
            GuestName = null,
            GuestPhone = null
        });

        Assert.Equal("anonymous_token", result.Token);
        _jwtMock.Verify(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), "Guest"), Times.Once);
    }

    [Fact]
    public async Task LoginGuest_NullGuestName_StoresDefaultGuestNameInSession()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);

        DiningSession? captured = null;
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .Callback<DiningSession>(s => captured = s)
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), "Guest"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest { TableId = 1 });

        // GuestName mặc định là "Guest" (không còn null) sau khi sửa LoginGuestAsync; GuestPhone vẫn null vì không truyền.
        Assert.Equal("Guest", captured!.GuestName);
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
        // Token vẫn được tạo với guestName = "" thay vì "Guest".
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), ""))
            .Returns(("empty_name_token", "jti"));

        var result = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 1,
            GuestName = "",
            GuestPhone = ""
        });

        // Xác nhận chuỗi rỗng đi xuyên qua không đổi thành "Guest" — tên hiển thị rỗng cho nhân viên/khác.
        _jwtMock.Verify(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), ""), Times.Once);
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

        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), 200, "Khách 1"))
            .Returns(("token_1", "jti_1"));

        var result1 = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 5, GuestName = "Khách 1"
        });

        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), 200, "Khách 2"))
            .Returns(("token_2", "jti_2"));

        var result2 = await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 5, GuestName = "Khách 2"
        });

        Assert.Equal(200, result1.SessionId);
        Assert.Equal(200, result2.SessionId);
        Assert.NotEqual(result1.Token, result2.Token);

        // BUG FIX VERIFIED: trước đây cả 2 khách dùng chung sub=sessionId nên không phân biệt được.
        // Giờ mỗi lần gọi GenerateGuestToken nhận 1 guestUniqueId (UUID) khác nhau làm sub.
        _jwtMock.Verify(j => j.GenerateGuestToken(It.IsAny<string>(), 200, "Khách 1"), Times.Once);
        _jwtMock.Verify(j => j.GenerateGuestToken(It.IsAny<string>(), 200, "Khách 2"), Times.Once);
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
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
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
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
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
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
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

    // ===== BUG FIX VERIFIED: GenerateGuestToken không còn nhận tham số email =====
    // Trước đây GenerateAccessToken(sessionId, "", name, "GUEST") luôn nhúng email rỗng vào JWT claim.
    // Thiết kế mới (GenerateGuestToken) bỏ hẳn khái niệm email cho GUEST — không còn claim email rỗng.

    [Fact]
    public async Task LoginGuest_TokenGenerationNoLongerTakesEmailParameter()
    {
        var table = new Table { Id = 1, TableNumber = 1, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(1)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => s);
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), "Khách"))
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 1, GuestName = "Khách"
        });

        _jwtMock.Verify(j => j.GenerateGuestToken(It.IsAny<string>(), It.IsAny<int>(), "Khách"), Times.Once);
        _jwtMock.Verify(j => j.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), "GUEST"), Times.Never);
    }

    // ===== BUG FIX VERIFIED + BUG MỚI PHÁT SINH: sub giờ là UUID, không phải sessionId =====

    [Fact]
    public async Task LoginGuest_UsesUuidAsIdentifier_NotSessionId()
    {
        var table = new Table { Id = 5, TableNumber = 12, Status = TableStatus.AVAILABLE };
        _tableRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(table);
        _sessionRepoMock.Setup(r => r.GetActiveByTableIdAsync(5)).ReturnsAsync((DiningSession?)null);
        _sessionRepoMock.Setup(r => r.AddAsync(It.IsAny<DiningSession>()))
            .ReturnsAsync((DiningSession s) => { s.Id = 77; return s; });

        string? capturedUuid = null;
        _jwtMock.Setup(j => j.GenerateGuestToken(It.IsAny<string>(), 77, "Khách"))
            .Callback<string, int, string>((uuid, _, _) => capturedUuid = uuid)
            .Returns(("token", "jti"));

        await _authService.LoginGuestAsync(new GuestLoginRequest
        {
            TableId = 5, GuestName = "Khách"
        });

        // FIXED: sub (capturedUuid) không còn trùng với sessionId (77) — mỗi GUEST có 1 UUID riêng.
        Assert.NotNull(capturedUuid);
        Assert.NotEqual("77", capturedUuid);
        Assert.True(Guid.TryParse(capturedUuid, out _), "guestUniqueId phải là 1 GUID hợp lệ");

        // sub (capturedUuid) không phải số nguyên — đây là thiết kế có chủ đích. AuthController
        // (cả SmartDine.API và SmartDine.Identity.API) đã được sửa để không còn int.Parse(sub) vô
        // điều kiện: với GUEST, controller đọc id thực tế từ custom claim "session_id" thay vì sub.
        // Xem JwtTokenServiceTests.GenerateGuestToken_SubClaim_IsNotParsableAsInt_ControllersMustUseSessionIdClaimInstead.
        Assert.False(int.TryParse(capturedUuid, out _),
            "sub là UUID, không phải sessionId — AuthController phải lấy id từ claim \"session_id\" khi role=GUEST");
    }
}
