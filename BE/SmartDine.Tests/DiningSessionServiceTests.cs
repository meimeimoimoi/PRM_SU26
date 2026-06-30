using Moq;
using SmartDine.Application.Services;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Tests;

public class DiningSessionServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IDiningSessionRepository> _sessionRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IRepository<SessionParticipant>> _participantRepoMock;
    private readonly DiningSessionService _service;

    public DiningSessionServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _participantRepoMock = new Mock<IRepository<SessionParticipant>>();

        _uowMock.Setup(u => u.DiningSessions).Returns(_sessionRepoMock.Object);
        _uowMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _uowMock.Setup(u => u.SessionParticipants).Returns(_participantRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new DiningSessionService(_uowMock.Object);
    }

    private static DiningSession BuildSession(int id = 1, int tableNumber = 10)
        => new()
        {
            Id = id,
            TableId = 1,
            Table = new Table { Id = 1, TableNumber = tableNumber },
            Status = DiningSessionStatus.ACTIVE
        };

    // ═══════════════════════════════════════════════════════════════
    // GetParticipantsAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetParticipants_SessionNotFound_ThrowsEntityNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(99)).ReturnsAsync((DiningSession?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _service.GetParticipantsAsync(99, null, null, isStaff: true));
    }

    [Fact]
    public async Task GetParticipants_ExcludesParticipantsWhoLeft()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { Id = 1, CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow.AddMinutes(-10) });
        session.Participants.Add(new SessionParticipant { Id = 2, CustomerId = 2, Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow.AddMinutes(-5), LeftAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var result = await _service.GetParticipantsAsync(1, 1, null, isStaff: false);

        Assert.Single(result.Members);
        Assert.Equal(1, result.Members[0].UserId);
    }

    [Fact]
    public async Task GetParticipants_OrdersByJoinedAt()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { Id = 1, CustomerId = 2, Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow.AddMinutes(-1) });
        session.Participants.Add(new SessionParticipant { Id = 2, CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow.AddMinutes(-10) });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var result = await _service.GetParticipantsAsync(1, 1, null, isStaff: false);

        Assert.Equal(1, result.Members[0].UserId);
        Assert.Equal(2, result.Members[1].UserId);
    }

    [Fact]
    public async Task GetParticipants_CustomerParticipant_UsesCustomerFullName()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant
        {
            CustomerId = 5,
            Customer = new Customer { Id = 5, FullName = "Nguyễn Văn A" },
            Role = ParticipantRole.HOST,
            JoinedAt = DateTime.UtcNow
        });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var result = await _service.GetParticipantsAsync(1, 5, null, isStaff: false);

        Assert.Equal("Nguyễn Văn A", result.Members[0].Name);
        Assert.Equal("HOST", result.Members[0].Role);
    }

    [Fact]
    public async Task GetParticipants_GuestParticipant_UsesDefaultNameWithIndex()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { GuestSessionId = "uuid-1", Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow.AddMinutes(-2) });
        session.Participants.Add(new SessionParticipant { GuestSessionId = "uuid-2", Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow.AddMinutes(-1) });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var result = await _service.GetParticipantsAsync(1, null, "uuid-1", isStaff: false);

        Assert.Equal("Khách 1", result.Members[0].Name);
        Assert.Equal("Khách 2", result.Members[1].Name);
        Assert.Null(result.Members[0].UserId);
    }

    [Fact]
    public async Task GetParticipants_ReturnsCorrectTableNumber()
    {
        var session = BuildSession(tableNumber: 42);
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var result = await _service.GetParticipantsAsync(1, null, null, isStaff: true);

        Assert.Equal(42, result.TableNumber);
    }

    [Fact]
    public async Task GetParticipants_StaffCaller_CanViewAnySessionWithoutBeingParticipant()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 999, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        // STAFF không phải participant nào của session nhưng vẫn được xem (đúng nghiệp vụ).
        var result = await _service.GetParticipantsAsync(1, null, null, isStaff: true);

        Assert.NotEmpty(result.Members);
    }

    // BUG FIX VERIFIED: trước đây GetParticipantsAsync không kiểm tra caller có thuộc session
    // hay không, nên CUSTOMER/GUEST bất kỳ có thể xem participants của bàn khác (IDOR) chỉ bằng
    // cách đổi {id} trên URL. Đã thêm EnsureCallerCanView — giờ CUSTOMER không thuộc session bị
    // chặn bằng UnauthorizedAccessException.
    [Fact]
    public async Task GetParticipants_CustomerNotInSession_ThrowsUnauthorized()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 999, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        // customerId=123 không phải participant nào của session 1
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetParticipantsAsync(1, 123, null, isStaff: false));
    }

    [Fact]
    public async Task GetParticipants_GuestNotInSession_ThrowsUnauthorized()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { GuestSessionId = "real-guest", Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetParticipantsAsync(1, null, "fake-guest-uuid", isStaff: false));
    }

    [Fact]
    public async Task GetParticipants_ParticipantWhoAlreadyLeft_ThrowsUnauthorized()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 7, Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow, LeftAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        // Đã rời session rồi thì không còn quyền xem nữa.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetParticipantsAsync(1, 7, null, isStaff: false));
    }

    // ═══════════════════════════════════════════════════════════════
    // LeaveSessionAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task LeaveSession_SessionNotFound_ThrowsEntityNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(99)).ReturnsAsync((DiningSession?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.LeaveSessionAsync(99, 1, null));
    }

    [Fact]
    public async Task LeaveSession_ParticipantNotFound_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.LeaveSessionAsync(1, 777, null));
        Assert.Contains("không thuộc phiên ăn này", ex.Message);
    }

    [Fact]
    public async Task LeaveSession_AlreadyLeft_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow, LeftAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.LeaveSessionAsync(1, 1, null));
        Assert.Contains("đã rời", ex.Message);
    }

    [Fact]
    public async Task LeaveSession_MemberLeaves_NoHostTransfer()
    {
        var session = BuildSession(tableNumber: 7);
        session.Participants.Add(new SessionParticipant { Id = 1, CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow.AddMinutes(-10) });
        session.Participants.Add(new SessionParticipant { Id = 2, CustomerId = 2, Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow.AddMinutes(-5) });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var result = await _service.LeaveSessionAsync(1, 2, null);

        Assert.Null(result.NewHostId);
        Assert.Contains("7", result.Message);
        Assert.NotNull(session.Participants.First(p => p.CustomerId == 2).LeftAt);
        Assert.Equal(ParticipantRole.HOST, session.Participants.First(p => p.CustomerId == 1).Role);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveSession_HostLeaves_TransfersToOldestRemainingActiveParticipant()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { Id = 1, CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow.AddMinutes(-10) });
        session.Participants.Add(new SessionParticipant { Id = 2, CustomerId = 2, Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow.AddMinutes(-8), LeftAt = DateTime.UtcNow });
        session.Participants.Add(new SessionParticipant { Id = 3, CustomerId = 3, Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow.AddMinutes(-5) });

        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var result = await _service.LeaveSessionAsync(1, 1, null);

        // Participant #2 đã rời từ trước (LeftAt != null) nên KHÔNG được chọn làm HOST mới,
        // dù vào trước participant #3.
        Assert.Equal(3, result.NewHostId);
        Assert.Equal(ParticipantRole.HOST, session.Participants.First(p => p.CustomerId == 3).Role);
    }

    [Fact]
    public async Task LeaveSession_HostLeaves_LastParticipant_NewHostIdNull()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { Id = 1, CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var result = await _service.LeaveSessionAsync(1, 1, null);

        Assert.Null(result.NewHostId);
    }

    [Fact]
    public async Task LeaveSession_GuestIdentifiedByGuestSessionId_Success()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow.AddMinutes(-5) });
        session.Participants.Add(new SessionParticipant { GuestSessionId = "guest-uuid-123", Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var result = await _service.LeaveSessionAsync(1, null, "guest-uuid-123");

        Assert.Null(result.NewHostId);
        Assert.NotNull(session.Participants.First(p => p.GuestSessionId == "guest-uuid-123").LeftAt);
    }

    [Fact]
    public async Task LeaveSession_BothIdentitiesNull_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => _service.LeaveSessionAsync(1, null, null));
    }

    // BUG DETECTION: khi customerId được truyền vào (có giá trị, dù không khớp ai),
    // FindParticipant CHỈ tìm theo CustomerId và bỏ qua hoàn toàn guestSessionId — kể cả khi
    // guestSessionId thực ra khớp với 1 participant. Trong thực tế controller chỉ gửi 1 trong 2
    // (CUSTOMER hoặc GUEST) nên hiếm khi xảy ra, nhưng API tầng service không tự bảo vệ trước input sai.
    [Fact]
    public async Task LeaveSession_CustomerIdProvidedButNoMatch_IgnoresGuestSessionIdEvenIfItWouldMatch_PotentialBug()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { GuestSessionId = "guest-uuid-123", Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        // customerId=777 không khớp ai, NHƯNG guestSessionId="guest-uuid-123" lẽ ra khớp.
        // FindParticipant ưu tiên tuyệt đối customerId.HasValue nên vẫn throw "không thuộc phiên ăn này".
        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.LeaveSessionAsync(1, 777, "guest-uuid-123"));
    }

    // ═══════════════════════════════════════════════════════════════
    // GetBillSummaryAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetBillSummary_SessionNotFound_ThrowsEntityNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(99)).ReturnsAsync((DiningSession?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _service.GetBillSummaryAsync(99, null, null, isStaff: true));
    }

    [Fact]
    public async Task GetBillSummary_NoOrders_ReturnsZero()
    {
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(BuildSession());
        _orderRepoMock.Setup(r => r.GetByDiningSessionIdAsync(1)).ReturnsAsync(new List<Order>());

        var result = await _service.GetBillSummaryAsync(1, null, null, isStaff: true);

        Assert.Equal(0, result.SubTotal);
        Assert.Equal(0, result.Tax);
        Assert.Equal(0, result.EstimatedTotal);
    }

    [Fact]
    public async Task GetBillSummary_ExcludesCancelledOrders_Calculates10PercentTax()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _orderRepoMock.Setup(r => r.GetByDiningSessionIdAsync(1)).ReturnsAsync(new List<Order>
        {
            new() { Id = 1, FinalAmount = 100_000m, Status = OrderStatus.COMPLETED },
            new() { Id = 2, FinalAmount = 50_000m, Status = OrderStatus.PENDING },
            new() { Id = 3, FinalAmount = 200_000m, Status = OrderStatus.CANCELLED },
        });

        var result = await _service.GetBillSummaryAsync(1, 1, null, isStaff: false);

        Assert.Equal(150_000m, result.SubTotal);
        Assert.Equal(15_000m, result.Tax);
        Assert.Equal(165_000m, result.EstimatedTotal);
    }

    // BUG FIX VERIFIED: trước đây GetBillSummaryAsync không kiểm tra caller có thuộc session
    // hay không, nên CUSTOMER/GUEST bất kỳ có thể xem bill của bàn khác (IDOR). Đã thêm
    // EnsureCallerCanView — giờ CUSTOMER không thuộc session bị chặn.
    [Fact]
    public async Task GetBillSummary_CustomerNotInSession_ThrowsUnauthorized()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _orderRepoMock.Setup(r => r.GetByDiningSessionIdAsync(1)).ReturnsAsync(new List<Order>
        {
            new() { Id = 1, FinalAmount = 500_000m, Status = OrderStatus.COMPLETED }
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetBillSummaryAsync(1, 999, null, isStaff: false));
    }

    [Fact]
    public async Task GetBillSummary_StaffCaller_CanViewAnySessionWithoutBeingParticipant()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _orderRepoMock.Setup(r => r.GetByDiningSessionIdAsync(1)).ReturnsAsync(new List<Order>
        {
            new() { Id = 1, FinalAmount = 500_000m, Status = OrderStatus.COMPLETED }
        });

        var result = await _service.GetBillSummaryAsync(1, null, null, isStaff: true);

        Assert.Equal(500_000m, result.SubTotal);
    }

    // ═══════════════════════════════════════════════════════════════
    // GetSessionOrdersAsync
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSessionOrders_SessionNotFound_ThrowsEntityNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(99)).ReturnsAsync((DiningSession?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _service.GetSessionOrdersAsync(99, null, null, isStaff: true));
    }

    [Fact]
    public async Task GetSessionOrders_ReturnsOrdersWithItemDetails()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        var order = new Order
        {
            Id = 10,
            Status = OrderStatus.COOKING,
            FinalAmount = 75_000m,
            OrderDetails = new List<OrderDetail>
            {
                new()
                {
                    MenuItemId = 1,
                    MenuItem = new MenuItem { Id = 1, Name = "Phở bò" },
                    Quantity = 2,
                    Status = OrderDetailStatus.DOING
                }
            }
        };
        _orderRepoMock.Setup(r => r.GetByDiningSessionIdAsync(1)).ReturnsAsync(new List<Order> { order });

        var result = await _service.GetSessionOrdersAsync(1, 1, null, isStaff: false);

        Assert.Single(result.Orders);
        Assert.Equal("COOKING", result.Orders[0].OrderStatus);
        Assert.Equal(75_000m, result.Orders[0].FinalAmount);
        Assert.Equal("Phở bò", result.Orders[0].Items[0].Name);
        Assert.Equal(2, result.Orders[0].Items[0].Quantity);
    }

    [Fact]
    public async Task GetSessionOrders_MenuItemNull_UsesUnknownPlaceholder()
    {
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(BuildSession());
        var order = new Order
        {
            Id = 10,
            Status = OrderStatus.PENDING,
            FinalAmount = 0m,
            OrderDetails = new List<OrderDetail>
            {
                new() { MenuItemId = 999, MenuItem = null!, Quantity = 1 }
            }
        };
        _orderRepoMock.Setup(r => r.GetByDiningSessionIdAsync(1)).ReturnsAsync(new List<Order> { order });

        var result = await _service.GetSessionOrdersAsync(1, null, null, isStaff: true);

        Assert.Equal("Unknown", result.Orders[0].Items[0].Name);
    }

    [Fact]
    public async Task GetSessionOrders_NoOrders_ReturnsEmptyList()
    {
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(BuildSession());
        _orderRepoMock.Setup(r => r.GetByDiningSessionIdAsync(1)).ReturnsAsync(new List<Order>());

        var result = await _service.GetSessionOrdersAsync(1, null, null, isStaff: true);

        Assert.Empty(result.Orders);
    }

    // BUG FIX VERIFIED: trước đây GetSessionOrdersAsync không kiểm tra caller có thuộc session
    // hay không. Đã thêm EnsureCallerCanView — GUEST không thuộc session bị chặn.
    [Fact]
    public async Task GetSessionOrders_GuestNotInSession_ThrowsUnauthorized()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { GuestSessionId = "real-guest", Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _orderRepoMock.Setup(r => r.GetByDiningSessionIdAsync(1)).ReturnsAsync(new List<Order>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetSessionOrdersAsync(1, null, "fake-guest-uuid", isStaff: false));
    }
}
