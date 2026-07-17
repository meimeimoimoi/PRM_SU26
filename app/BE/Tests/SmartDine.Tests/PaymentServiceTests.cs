using Moq;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Payments;
using SmartDine.Application.Services;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;
using System.Text.Json;

namespace SmartDine.Tests;

/// <summary>
/// Unit tests cho PaymentService — bao gồm CreateIntentAsync và HandleWebhookAsync.
/// Dùng Mock để tách biệt hoàn toàn khỏi DB và PayOS network.
/// </summary>
public class PaymentServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IDiningSessionRepository> _sessionRepoMock;
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<ITableRepository> _tableRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<IRepository<LoyaltyTransaction>> _loyaltyRepoMock;
    private readonly Mock<ISettingsRepository> _settingsRepoMock;
    private readonly Mock<IPaymentGateway> _gatewayMock;
    private readonly Mock<IOrderNotificationService> _notificationMock;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _sessionRepoMock = new Mock<IDiningSessionRepository>();
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _tableRepoMock = new Mock<ITableRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _loyaltyRepoMock = new Mock<IRepository<LoyaltyTransaction>>();
        _settingsRepoMock = new Mock<ISettingsRepository>();
        _gatewayMock = new Mock<IPaymentGateway>();
        _notificationMock = new Mock<IOrderNotificationService>();

        _uowMock.Setup(u => u.DiningSessions).Returns(_sessionRepoMock.Object);
        _uowMock.Setup(u => u.Payments).Returns(_paymentRepoMock.Object);
        _uowMock.Setup(u => u.Tables).Returns(_tableRepoMock.Object);
        _uowMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
        _uowMock.Setup(u => u.LoyaltyTransactions).Returns(_loyaltyRepoMock.Object);
        _uowMock.Setup(u => u.Settings).Returns(_settingsRepoMock.Object);
        _uowMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        // Mặc định: VAT 8%, không phí DV — khớp seed cũ để assert ổn định
        _settingsRepoMock.Setup(r => r.GetSingletonAsync()).ReturnsAsync(new RestaurantSettings
        {
            TaxRate = 8.00m,
            ServiceChargeRate = 0m
        });

        // DB sequence fallback trong InMemory — trả về số cố định để test dễ assert
        _paymentRepoMock.Setup(r => r.GetNextOrderCodeAsync()).ReturnsAsync(123456L);

        // Notification mock: không throw, chỉ cần verify được gọi hay không
        _notificationMock.Setup(n => n.NotifyPaymentSuccessAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);
        _notificationMock.Setup(n => n.NotifyCashPaymentPendingAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);

        _service = new PaymentService(_uowMock.Object, _gatewayMock.Object, _notificationMock.Object);
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers — tạo dữ liệu test giả lập thực tế nhà hàng
    // ═══════════════════════════════════════════════════════════════

    private static DiningSession MakeSession(
        int id = 1,
        DiningSessionStatus status = DiningSessionStatus.ACTIVE,
        int customerId = 10,
        int tableId = 5,
        decimal orderTotal = 200_000m)
    {
        var session = new DiningSession
        {
            Id = id,
            Status = status,
            CustomerId = customerId,
            TableId = tableId,
            Table = new Table { Id = tableId, TableNumber = 5, Status = TableStatus.OCCUPIED },
            Participants = new List<SessionParticipant>
            {
                // LeftAt = null → IsActive = true (computed property)
                new() { CustomerId = customerId, LeftAt = null, Role = ParticipantRole.HOST, JoinedAt = DateTime.UtcNow }
            },
            Orders = new List<Order>
            {
                new() { Id = 1, Status = OrderStatus.COMPLETED, FinalAmount = orderTotal, TotalAmount = orderTotal }
            }
        };
        return session;
    }

    private static GatewayCreatePaymentResult GatewayOk(long orderCode = 123456) =>
        new(true, "https://pay.payos.vn/web/abc", "qrraw", "linkId", orderCode);

    private static GatewayCreatePaymentResult GatewayFail() =>
        new(false, null, null, null, 0, "Lỗi cổng thanh toán");

    // ═══════════════════════════════════════════════════════════════
    // CreateIntentAsync — thành công
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateIntent_Customer_Success_Returns_InvoiceAndQr()
    {
        // Thực tế: DINER bấm "Thanh toán" sau bữa ăn → nhận QR để quét
        var session = MakeSession(customerId: 10);
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);
        _paymentRepoMock.Setup(r => r.GetBySessionIdAsync(1)).ReturnsAsync((Payment?)null);
        _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _gatewayMock.Setup(g => g.CreatePaymentLinkAsync(
            It.IsAny<long>(), 216000, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GatewayOk());

        var request = new CreatePaymentIntentRequest { SessionId = 1, PaymentMethod = "VNPAY", SplitCount = 1 };
        var result = await _service.CreateIntentAsync(10, null, false, request);

        Assert.NotEmpty(result.InvoiceId);
        Assert.StartsWith("INV-", result.InvoiceId);
        Assert.Equal(216_000m, result.TotalPayable); // 200k + VAT 8%
        Assert.NotNull(result.QrUrl);

        // Session phải được đặt về CHECKOUT
        Assert.Equal(DiningSessionStatus.CHECKOUT, session.Status);
        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateIntent_Guest_Success_QrReturned()
    {
        // Thực tế: GUEST (không có tài khoản) cũng có thể thanh toán
        var session = MakeSession(customerId: 10);
        // GUEST participant thêm vào cùng session
        session.Participants.Add(new SessionParticipant
        {
            CustomerId = null,
            GuestSessionId = "uuid-guest-abc",
            LeftAt = null, // IsActive = true
            Role = ParticipantRole.MEMBER,
            JoinedAt = DateTime.UtcNow
        });

        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);
        _paymentRepoMock.Setup(r => r.GetBySessionIdAsync(1)).ReturnsAsync((Payment?)null);
        _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _gatewayMock.Setup(g => g.CreatePaymentLinkAsync(
            It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GatewayOk());

        var result = await _service.CreateIntentAsync(null, "uuid-guest-abc", false,
            new CreatePaymentIntentRequest { SessionId = 1, PaymentMethod = "VNPAY" });

        Assert.NotEmpty(result.InvoiceId);
        Assert.Equal(216_000m, result.TotalPayable);
    }

    [Fact]
    public async Task CreateIntent_Staff_Bypasses_Ownership_Check()
    {
        // Thực tế: nhân viên thanh toán hộ khách — không cần là participant
        var session = MakeSession(customerId: 10);
        session.Participants.Clear(); // không có participant nào match staff

        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);
        _paymentRepoMock.Setup(r => r.GetBySessionIdAsync(1)).ReturnsAsync((Payment?)null);
        _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);
        _gatewayMock.Setup(g => g.CreatePaymentLinkAsync(
            It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GatewayOk());

        // isStaff = true → không cần participant
        var result = await _service.CreateIntentAsync(null, null, true,
            new CreatePaymentIntentRequest { SessionId = 1, PaymentMethod = "CASH" });

        Assert.NotNull(result);
        Assert.Equal(216_000m, result.TotalPayable);
        Assert.Equal(DiningSessionStatus.CHECKOUT, session.Status);
        _gatewayMock.Verify(g => g.CreatePaymentLinkAsync(
            It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _notificationMock.Verify(n => n.NotifyCashPaymentPendingAsync(5, 5, It.IsAny<string>(), 216_000m), Times.Once);
    }

    // ═══════════════════════════════════════════════════════════════
    // CreateIntentAsync — lỗi session
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateIntent_SessionNotFound_Throws_EntityNotFoundException()
    {
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(99)).ReturnsAsync((DiningSession?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.CreateIntentAsync(10, null, false,
                new CreatePaymentIntentRequest { SessionId = 99 }));
    }

    [Fact]
    public async Task CreateIntent_SessionCheckout_Throws_BusinessRule()
    {
        // Thực tế: bấm "Thanh toán" 2 lần trong vài giây — session đã CHECKOUT từ lần 1
        var session = MakeSession(status: DiningSessionStatus.CHECKOUT);
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateIntentAsync(10, null, false,
                new CreatePaymentIntentRequest { SessionId = 1 }));

        Assert.Equal(ValidationMessages.PAYMENT_SESSION_CHECKOUT_IN_PROGRESS, ex.Message);
    }

    [Fact]
    public async Task CreateIntent_SessionClosed_Throws_BusinessRule()
    {
        var session = MakeSession(status: DiningSessionStatus.CLOSED);
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateIntentAsync(10, null, false,
                new CreatePaymentIntentRequest { SessionId = 1 }));

        Assert.Equal(ValidationMessages.PAYMENT_SESSION_CLOSED, ex.Message);
    }

    [Fact]
    public async Task CreateIntent_NonParticipant_Customer_Throws_Unauthorized()
    {
        // Thực tế: khách bàn khác thử thanh toán hộ — IDOR attempt
        var session = MakeSession(customerId: 10);
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);
        _paymentRepoMock.Setup(r => r.GetBySessionIdAsync(1)).ReturnsAsync((Payment?)null);

        // callerCustomerId = 99 (không phải participant)
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateIntentAsync(99, null, false,
                new CreatePaymentIntentRequest { SessionId = 1 }));
    }

    [Fact]
    public async Task CreateIntent_NoOrders_Throws_BusinessRule()
    {
        // Thực tế: bàn mới scan QR, chưa gọi món, bấm "Thanh toán" ngay
        var session = MakeSession(orderTotal: 0);
        session.Orders.Clear();
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);
        _paymentRepoMock.Setup(r => r.GetBySessionIdAsync(1)).ReturnsAsync((Payment?)null);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateIntentAsync(10, null, false,
                new CreatePaymentIntentRequest { SessionId = 1 }));

        Assert.Equal(ValidationMessages.PAYMENT_NO_ORDERS, ex.Message);
    }

    [Fact]
    public async Task CreateIntent_ExistingPendingPayment_Throws_BusinessRule()
    {
        // Thực tế: QR đã hiện nhưng khách chưa quét — bấm lại "Thanh toán" lần 2
        var session = MakeSession();
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);
        _paymentRepoMock.Setup(r => r.GetBySessionIdAsync(1))
            .ReturnsAsync(new Payment { PaymentStatus = PaymentStatus.PENDING });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateIntentAsync(10, null, false,
                new CreatePaymentIntentRequest { SessionId = 1 }));

        Assert.Equal(ValidationMessages.PAYMENT_ALREADY_PENDING, ex.Message);
    }

    [Fact]
    public async Task CreateIntent_AlreadyPaid_Throws_BusinessRule()
    {
        // Thực tế: bàn đã thanh toán rồi nhưng app chưa refresh → bấm lại
        var session = MakeSession();
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);
        _paymentRepoMock.Setup(r => r.GetBySessionIdAsync(1))
            .ReturnsAsync(new Payment { PaymentStatus = PaymentStatus.SUCCESS });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateIntentAsync(10, null, false,
                new CreatePaymentIntentRequest { SessionId = 1 }));

        Assert.Equal(ValidationMessages.PAYMENT_ALREADY_COMPLETED, ex.Message);
    }

    [Fact]
    public async Task CreateIntent_GatewayFails_Throws_BusinessRule()
    {
        // Thực tế: PayOS server down / rate limit → báo lỗi cho khách thử lại
        var session = MakeSession();
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAndOrdersAsync(1)).ReturnsAsync(session);
        _paymentRepoMock.Setup(r => r.GetBySessionIdAsync(1)).ReturnsAsync((Payment?)null);
        _gatewayMock.Setup(g => g.CreatePaymentLinkAsync(
            It.IsAny<long>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(GatewayFail());

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.CreateIntentAsync(10, null, false,
                new CreatePaymentIntentRequest { SessionId = 1 }));

        Assert.Equal(ValidationMessages.PAYMENT_GATEWAY_ERROR, ex.Message);
    }

    // ═══════════════════════════════════════════════════════════════
    // HandleWebhookAsync — thanh toán thành công
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleWebhook_Success_ClosesSession_FreesTable_AwardsPoints()
    {
        // Thực tế: PayOS gọi webhook → đóng bàn + tích điểm CUSTOMER
        var payment = new Payment
        {
            Id = 1, SessionId = 1, Amount = 200_000m,
            PaymentStatus = PaymentStatus.PENDING,
            ExternalRef = "123456",
            InvoiceId = "INV-2026-123456"
        };
        var session = MakeSession(tableId: 5, customerId: 10);
        var table = new Table { Id = 5, Status = TableStatus.OCCUPIED };
        var customer = new Customer { Id = 10, LoyaltyPoints = 0, TotalSpent = 0, VisitCount = 0, MembershipLevel = LoyaltyTier.BRONZE };

        _paymentRepoMock.Setup(r => r.GetByExternalRefAsync("123456")).ReturnsAsync(payment);
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);
        _tableRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(table);
        _customerRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(customer);
        _loyaltyRepoMock.Setup(r => r.AddAsync(It.IsAny<LoyaltyTransaction>()))
            .ReturnsAsync((LoyaltyTransaction lt) => lt);

        var webhookData = new GatewayWebhookData(123456, 200000, "INV-2026-123456", true);
        _gatewayMock.Setup(g => g.VerifyAndParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookData);

        var result = await _service.HandleWebhookAsync("{}", null);

        Assert.Equal("00", result.RspCode);
        Assert.Equal(PaymentStatus.SUCCESS, payment.PaymentStatus);
        Assert.Equal(DiningSessionStatus.CLOSED, session.Status);
        Assert.Equal(TableStatus.MAINTENANCE, table.Status);

        // 200.000 VND → 200 điểm (200.000 / 1.000 * 1)
        Assert.Equal(200, customer.LoyaltyPoints);
        Assert.Equal(200_000m, customer.TotalSpent);
        Assert.Equal(1, customer.VisitCount);

        _loyaltyRepoMock.Verify(r => r.AddAsync(It.Is<LoyaltyTransaction>(
            lt => lt.Points == 200 && lt.TransactionType == LoyaltyTransactionType.EARN)), Times.Once);

        // WebSocket thông báo về bàn 5 (TableNumber=0 vì test double không set field này)
        _notificationMock.Verify(n => n.NotifyPaymentSuccessAsync(5, 0, "INV-2026-123456", 200_000m), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_Success_GuestSession_NoLoyaltyPoints()
    {
        // Thực tế: GUEST không có tài khoản → không tích điểm (participant.CustomerId = null)
        var payment = new Payment
        {
            Id = 2, SessionId = 2, Amount = 150_000m,
            PaymentStatus = PaymentStatus.PENDING,
            ExternalRef = "999"
        };
        var session = MakeSession(id: 2, tableId: 7, customerId: 0);
        // Đặt lại participants chỉ có GUEST
        session.Participants = new List<SessionParticipant>
        {
            new() { CustomerId = null, GuestSessionId = "guest-uuid", LeftAt = null, Role = ParticipantRole.HOST }
        };
        var table = new Table { Id = 7, Status = TableStatus.OCCUPIED };

        _paymentRepoMock.Setup(r => r.GetByExternalRefAsync("999")).ReturnsAsync(payment);
        _sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(2)).ReturnsAsync(session);
        _tableRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(table);

        _gatewayMock.Setup(g => g.VerifyAndParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new GatewayWebhookData(999, 150000, "INV-2026-000999", true));

        var result = await _service.HandleWebhookAsync("{}", null);

        Assert.Equal("00", result.RspCode);
        Assert.Equal(TableStatus.MAINTENANCE, table.Status);
        // Không tích điểm
        _loyaltyRepoMock.Verify(r => r.AddAsync(It.IsAny<LoyaltyTransaction>()), Times.Never);
        _customerRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        // Vẫn bắn WebSocket thông báo về bàn (GUEST không có điểm nhưng bàn vẫn nhận event)
        _notificationMock.Verify(n => n.NotifyPaymentSuccessAsync(7, 0, It.IsAny<string>(), 150_000m), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_InvalidSignature_Throws_BusinessRule()
    {
        // Thực tế: ai đó giả mạo webhook gọi đến để đánh lừa hệ thống mark payment SUCCESS
        _gatewayMock.Setup(g => g.VerifyAndParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((GatewayWebhookData?)null);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _service.HandleWebhookAsync("{fake}", "bad-signature"));

        Assert.Equal(ValidationMessages.PAYMENT_WEBHOOK_INVALID_SIGNATURE, ex.Message);
    }

    [Fact]
    public async Task HandleWebhook_PaymentNotFound_Throws_EntityNotFoundException()
    {
        // Thực tế: PayOS retry webhook cho orderCode đã expired / bị xóa khỏi DB
        _gatewayMock.Setup(g => g.VerifyAndParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new GatewayWebhookData(999, 100000, "desc", true));
        _paymentRepoMock.Setup(r => r.GetByExternalRefAsync("999")).ReturnsAsync((Payment?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _service.HandleWebhookAsync("{}", null));
    }

    [Fact]
    public async Task HandleWebhook_AlreadySuccess_Idempotent_NoDoubleProcess()
    {
        // Thực tế: PayOS retry webhook (network timeout) — không được xử lý 2 lần
        var payment = new Payment
        {
            Id = 3, SessionId = 1, Amount = 100_000m,
            PaymentStatus = PaymentStatus.SUCCESS, // đã xử lý trước đó
            ExternalRef = "777"
        };

        _gatewayMock.Setup(g => g.VerifyAndParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new GatewayWebhookData(777, 100000, "desc", true));
        _paymentRepoMock.Setup(r => r.GetByExternalRefAsync("777")).ReturnsAsync(payment);

        var result = await _service.HandleWebhookAsync("{}", null);

        Assert.Equal("00", result.RspCode);
        Assert.Equal("Already processed", result.Message);
        // Không update session, không cộng điểm
        _sessionRepoMock.Verify(r => r.GetByIdWithParticipantsAsync(It.IsAny<int>()), Times.Never);
        _loyaltyRepoMock.Verify(r => r.AddAsync(It.IsAny<LoyaltyTransaction>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task HandleWebhook_PaymentFailed_Reverts_Session_To_Active()
    {
        // Thực tế: khách hủy thanh toán trên app VNPay → session phải về ACTIVE để retry
        var payment = new Payment
        {
            Id = 4, SessionId = 1, Amount = 300_000m,
            PaymentStatus = PaymentStatus.PENDING,
            ExternalRef = "555"
        };
        var session = MakeSession(status: DiningSessionStatus.CHECKOUT);

        _gatewayMock.Setup(g => g.VerifyAndParseWebhook(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new GatewayWebhookData(555, 300000, "desc", false)); // IsSuccess = false
        _paymentRepoMock.Setup(r => r.GetByExternalRefAsync("555")).ReturnsAsync(payment);
        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);

        var result = await _service.HandleWebhookAsync("{}", null);

        Assert.Equal("00", result.RspCode);
        Assert.Equal(PaymentStatus.FAILED, payment.PaymentStatus);
        Assert.Equal(DiningSessionStatus.ACTIVE, session.Status); // revert → khách có thể retry

        // Bàn không được giải phóng (chưa thanh toán xong)
        _tableRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════
    // OrderService — block order khi CHECKOUT
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task PlaceOrder_SessionCheckout_Throws_BusinessRule()
    {
        // Thực tế: khách đang quét QR thanh toán, người khác trong nhóm cố gọi thêm món
        // → hệ thống phải từ chối để tránh số tiền thay đổi sau khi đã sinh QR
        var sessionRepoMock = new Mock<IDiningSessionRepository>();
        var orderRepoMock = new Mock<IOrderRepository>();
        var menuItemRepoMock = new Mock<IMenuItemRepository>();
        var couponRepoMock = new Mock<ICouponRepository>();
        var notificationMock = new Mock<IOrderNotificationService>();
        var uowMock = new Mock<IUnitOfWork>();

        uowMock.Setup(u => u.DiningSessions).Returns(sessionRepoMock.Object);
        uowMock.Setup(u => u.Orders).Returns(orderRepoMock.Object);
        uowMock.Setup(u => u.MenuItems).Returns(menuItemRepoMock.Object);
        uowMock.Setup(u => u.Coupons).Returns(couponRepoMock.Object);

        var orderService = new OrderService(uowMock.Object, notificationMock.Object);

        var session = MakeSession(status: DiningSessionStatus.CHECKOUT);
        sessionRepoMock.Setup(r => r.GetByIdWithParticipantsAsync(1)).ReturnsAsync(session);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            orderService.PlaceOrderAsync(10, null, false,
                new SmartDine.Application.DTOs.Orders.PlaceOrderRequest
                {
                    DiningSessionId = 1,
                    TableId = 5,
                    Items = new List<SmartDine.Application.DTOs.Orders.OrderDetailRequest>
                    {
                        new() { MenuItemId = 1, Quantity = 1 }
                    }
                }));

        Assert.Equal(ValidationMessages.ORDER_BLOCKED_CHECKOUT, ex.Message);
    }
}
