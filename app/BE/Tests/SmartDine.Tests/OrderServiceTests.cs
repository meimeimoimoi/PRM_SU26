using Moq;
using SmartDine.Application.DTOs.Orders;
using SmartDine.Application.Services;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Tests;

public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IDiningSessionRepository> _sessionRepoMock;
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IMenuItemRepository> _menuItemRepoMock;
    private readonly Mock<ICouponRepository> _couponRepoMock;
    private readonly Mock<IOrderNotificationService> _notificationMock;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _orderRepoMock = new Mock<IOrderRepository>();
        _menuItemRepoMock = new Mock<IMenuItemRepository>();
        _couponRepoMock = new Mock<ICouponRepository>();
        _notificationMock = new Mock<IOrderNotificationService>();

        _uowMock.Setup(u => u.DiningSessions).Returns(_sessionRepoMock.Object);
        _uowMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _uowMock.Setup(u => u.MenuItems).Returns(_menuItemRepoMock.Object);
        _uowMock.Setup(u => u.Coupons).Returns(_couponRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => o);

        _service = new OrderService(_uowMock.Object, _notificationMock.Object);
    }

    private static DiningSession BuildSession(int id = 1, int tableNumber = 10)
        => new()
        {
            Id = id,
            TableId = 1,
            Table = new Table { Id = 1, TableNumber = tableNumber },
            Status = DiningSessionStatus.ACTIVE
        };

    private static MenuItem BuildMenuItem(int id, decimal price = 50_000m, bool available = true)
        => new() { Id = id, Name = $"Món {id}", Price = price, IsAvailable = available };

    private static PlaceOrderRequest BuildRequest(int sessionId, string? couponCode = null)
        => new()
        {
            DiningSessionId = sessionId,
            CouponCode = couponCode,
            Items = new List<OrderDetailRequest> { new() { MenuItemId = 1, Quantity = 2 } }
        };

    // ═══════════════════════════════════════════════════════════════
    // PlaceOrderAsync — session & item validation
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task PlaceOrder_SessionNotFound_ThrowsEntityNotFound()
    {
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(99)).ReturnsAsync((DiningSession?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _service.PlaceOrderAsync(1, null, false, BuildRequest(99)));
    }

    [Fact]
    public async Task PlaceOrder_SessionNotActive_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Status = DiningSessionStatus.CLOSED;
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.PlaceOrderAsync(1, null, false, BuildRequest(1)));
    }

    [Fact]
    public async Task PlaceOrder_MenuItemNotFound_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<MenuItem>());

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.PlaceOrderAsync(1, null, false, BuildRequest(1)));
    }

    [Fact]
    public async Task PlaceOrder_MenuItemUnavailable_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1, available: false) });

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.PlaceOrderAsync(1, null, false, BuildRequest(1)));
    }

    // ═══════════════════════════════════════════════════════════════
    // PlaceOrderAsync — ownership (IDOR fix)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task PlaceOrder_CustomerNotParticipant_ThrowsUnauthorized()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 999, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.PlaceOrderAsync(123, null, false, BuildRequest(1)));
    }

    [Fact]
    public async Task PlaceOrder_GuestNotParticipant_ThrowsUnauthorized()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { GuestSessionId = "real-guest", Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.PlaceOrderAsync(null, "fake-guest-uuid", false, BuildRequest(1)));
    }

    [Fact]
    public async Task PlaceOrder_CustomerParticipant_Success()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });

        var result = await _service.PlaceOrderAsync(1, null, false, BuildRequest(1));

        Assert.Equal("PENDING", result.Status);
        Assert.Equal(100_000m, result.TotalAmount);
        Assert.Equal(10, result.TableNumber);
        _notificationMock.Verify(n => n.NotifyNewOrderAsync(It.IsAny<int>(), 10, 100_000m), Times.Once);
    }

    [Fact]
    public async Task PlaceOrder_GuestParticipant_Success()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { GuestSessionId = "guest-uuid", Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });

        var result = await _service.PlaceOrderAsync(null, "guest-uuid", false, BuildRequest(1));

        Assert.Equal("PENDING", result.Status);
        _couponRepoMock.Verify(c => c.GetActivePromotionByCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_StaffBypassesOwnershipCheck_Success()
    {
        var session = BuildSession();
        // Session không có participant nào trùng — STAFF vẫn đặt được.
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });

        var result = await _service.PlaceOrderAsync(null, null, true, BuildRequest(1));

        Assert.Equal("PENDING", result.Status);
    }

    // ═══════════════════════════════════════════════════════════════
    // PlaceOrderAsync — coupon
    // ═══════════════════════════════════════════════════════════════

    private static Promotion BuildPromotion(string code, PromotionType type = PromotionType.PERCENT, decimal value = 20)
        => new()
        {
            Id = 1,
            Code = code,
            DiscountType = type,
            DiscountValue = value,
            IsActive = true,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1)
        };

    [Fact]
    public async Task PlaceOrder_CouponNotFound_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });
        _couponRepoMock.Setup(c => c.GetActivePromotionByCodeAsync("BADCODE")).ReturnsAsync((Promotion?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.PlaceOrderAsync(1, null, false, BuildRequest(1, "BADCODE")));
    }

    [Fact]
    public async Task PlaceOrder_CouponExpired_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });

        var promotion = BuildPromotion("EXPIRED");
        promotion.EndDate = DateTime.UtcNow.AddDays(-1);
        _couponRepoMock.Setup(c => c.GetActivePromotionByCodeAsync("EXPIRED")).ReturnsAsync(promotion);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.PlaceOrderAsync(1, null, false, BuildRequest(1, "EXPIRED")));
    }

    [Fact]
    public async Task PlaceOrder_CouponNotOwned_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });
        _couponRepoMock.Setup(c => c.GetActivePromotionByCodeAsync("GIAM20K")).ReturnsAsync(BuildPromotion("GIAM20K"));
        _couponRepoMock.Setup(c => c.GetByCustomerAndPromotionAsync(1, 1)).ReturnsAsync((CustomerCoupon?)null);

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.PlaceOrderAsync(1, null, false, BuildRequest(1, "GIAM20K")));
    }

    [Fact]
    public async Task PlaceOrder_CouponAlreadyUsed_ThrowsBusinessRuleViolation()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });
        _couponRepoMock.Setup(c => c.GetActivePromotionByCodeAsync("GIAM20K")).ReturnsAsync(BuildPromotion("GIAM20K"));
        _couponRepoMock.Setup(c => c.GetByCustomerAndPromotionAsync(1, 1))
            .ReturnsAsync(new CustomerCoupon { CustomerId = 1, PromotionId = 1, IsUsed = true });

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => _service.PlaceOrderAsync(1, null, false, BuildRequest(1, "GIAM20K")));
    }

    [Fact]
    public async Task PlaceOrder_ValidPercentCoupon_AppliesDiscountAndMarksUsed()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });

        var promotion = BuildPromotion("GIAM20K", PromotionType.PERCENT, 20);
        var customerCoupon = new CustomerCoupon { CustomerId = 1, PromotionId = 1, IsUsed = false };
        _couponRepoMock.Setup(c => c.GetActivePromotionByCodeAsync("GIAM20K")).ReturnsAsync(promotion);
        _couponRepoMock.Setup(c => c.GetByCustomerAndPromotionAsync(1, 1)).ReturnsAsync(customerCoupon);

        var result = await _service.PlaceOrderAsync(1, null, false, BuildRequest(1, "GIAM20K"));

        // 2 x 50_000 = 100_000 → giảm 20% = 20_000 → còn 80_000
        Assert.Equal(100_000m, result.TotalAmount);
        Assert.Equal(20_000m, result.DiscountAmount);
        Assert.Equal(80_000m, result.FinalAmount);
        Assert.True(customerCoupon.IsUsed);
        Assert.NotNull(customerCoupon.UsedAt);
    }

    [Fact]
    public async Task PlaceOrder_GuestSendsCouponCode_SilentlyIgnored()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { GuestSessionId = "guest-uuid", Role = ParticipantRole.MEMBER, JoinedAt = DateTime.UtcNow });
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });

        var result = await _service.PlaceOrderAsync(null, "guest-uuid", false, BuildRequest(1, "GIAM20K"));

        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(100_000m, result.FinalAmount);
        _couponRepoMock.Verify(c => c.GetActivePromotionByCodeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_StaffSendsCouponCode_SilentlyIgnored()
    {
        var session = BuildSession();
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _menuItemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<MenuItem> { BuildMenuItem(1) });

        var result = await _service.PlaceOrderAsync(null, null, true, BuildRequest(1, "GIAM20K"));

        Assert.Equal(0m, result.DiscountAmount);
        _couponRepoMock.Verify(c => c.GetActivePromotionByCodeAsync(It.IsAny<string>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════
    // GetStatusAsync
    // ═══════════════════════════════════════════════════════════════

    private static Order BuildOrder(int id, DiningSession session, OrderStatus status = OrderStatus.COOKING)
        => new()
        {
            Id = id,
            SessionId = session.Id,
            Session = session,
            Status = status,
            OrderDetails = new List<OrderDetail>
            {
                new() { MenuItemId = 1, MenuItem = new MenuItem { Id = 1, Name = "Lẩu Thái" }, Quantity = 1, Status = OrderDetailStatus.DOING },
                new() { MenuItemId = 2, MenuItem = new MenuItem { Id = 2, Name = "Trà gừng" }, Quantity = 3, Status = OrderDetailStatus.DONE }
            }
        };

    [Fact]
    public async Task GetStatus_OrderNotFound_ThrowsEntityNotFound()
    {
        _orderRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => _service.GetStatusAsync(99, 1, null, false));
    }

    [Fact]
    public async Task GetStatus_Participant_ReturnsItemStatuses()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 1, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        var order = BuildOrder(4501, session);
        _orderRepoMock.Setup(r => r.GetByIdAsync(4501)).ReturnsAsync(order);

        var result = await _service.GetStatusAsync(4501, 1, null, false);

        Assert.Equal(4501, result.OrderId);
        Assert.Equal("COOKING", result.Status);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("DOING", result.Items[0].Status);
        Assert.Equal("DONE", result.Items[1].Status);
    }

    [Fact]
    public async Task GetStatus_Staff_BypassesOwnershipCheck()
    {
        var session = BuildSession();
        var order = BuildOrder(4501, session);
        _orderRepoMock.Setup(r => r.GetByIdAsync(4501)).ReturnsAsync(order);

        var result = await _service.GetStatusAsync(4501, null, null, true);

        Assert.Equal(4501, result.OrderId);
    }

    [Fact]
    public async Task GetStatus_NonParticipantCustomer_ThrowsUnauthorized()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { CustomerId = 999, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        var order = BuildOrder(4501, session);
        _orderRepoMock.Setup(r => r.GetByIdAsync(4501)).ReturnsAsync(order);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetStatusAsync(4501, 123, null, false));
    }

    [Fact]
    public async Task GetStatus_NonParticipantGuest_ThrowsUnauthorized()
    {
        var session = BuildSession();
        session.Participants.Add(new SessionParticipant { GuestSessionId = "real-guest", Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow });
        var order = BuildOrder(4501, session);
        _orderRepoMock.Setup(r => r.GetByIdAsync(4501)).ReturnsAsync(order);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GetStatusAsync(4501, null, "fake-guest-uuid", false));
    }
}
