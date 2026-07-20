using Microsoft.Extensions.Configuration;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.Payments;
using SmartDine.Application.Helper;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý nghiệp vụ thanh toán — tạo hóa đơn, gọi cổng thanh toán, xử lý webhook.
///
/// Luồng chính:
///   1. CreateIntentAsync: kiểm tra quyền → tính tổng → gọi PayOS → khóa session (CHECKOUT)
///   2. HandleWebhookAsync: xác minh chữ ký PayOS → cập nhật trạng thái → cộng điểm loyalty
///
/// Ai dùng: PaymentsController (Order.API).
/// Dependency: IUnitOfWork (DB), IPaymentGateway (PayOS/VNPay/Momo), IConfiguration (Payment:ReturnUrl/CancelUrl).
/// </summary>
public class PaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IPaymentGateway _gateway;
    private readonly IOrderNotificationService _notificationService;
    private readonly IConfiguration? _configuration;

    // Tỷ lệ tích điểm: 1 điểm / 1.000 VND — cân nhắc chuyển sang config nếu nhà hàng muốn điều chỉnh.
    private const int PointsPerThousandVnd = 1;

    // orderCode PayOS luôn dùng khi merchant bấm "xác nhận Webhook URL" trên dashboard — không
    // gắn với Payment thật nào trong hệ thống. Phải trả 200 ngay, nếu không PayOS sẽ báo webhook lỗi.
    private const long PayOsWebhookVerificationOrderCode = 123;

    public PaymentService(
        IUnitOfWork uow,
        IPaymentGateway gateway,
        IOrderNotificationService notificationService,
        IConfiguration? configuration = null)
    {
        _uow = uow;
        _gateway = gateway;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    /// <summary>URL khách được redirect sau thanh toán online thành công (PayOS).</summary>
    private string PaymentReturnUrl =>
        _configuration?["Payment:ReturnUrl"]?.Trim()
        ?? "https://smartdine.app/payment/success";

    /// <summary>URL khách được redirect khi hủy thanh toán online (PayOS).</summary>
    private string PaymentCancelUrl =>
        _configuration?["Payment:CancelUrl"]?.Trim()
        ?? "https://smartdine.app/payment/cancel";

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/payments/create-intent
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Khởi tạo phiên thanh toán: tính tổng hóa đơn → gọi PayOS → trả về QR/deeplink.
    ///
    /// Ownership: CUSTOMER/GUEST chỉ thanh toán session mình đang là participant.
    ///            STAFF thanh toán được mọi session (hỗ trợ thanh toán hộ).
    ///
    /// Sau khi gọi thành công: session.Status = CHECKOUT, không cho đặt thêm món.
    /// Nếu thanh toán thất bại (webhook FAIL): session quay về ACTIVE để retry.
    ///
    /// Ai dùng: PaymentsController.CreateIntent — DINER, GUEST, STAFF.
    /// </summary>
    public async Task<CreatePaymentIntentResponse> CreateIntentAsync(
        int? callerCustomerId,
        string? callerGuestSessionId,
        bool isStaff,
        CreatePaymentIntentRequest request)
    {
        // 1. Load session với đầy đủ Participants + Orders để tính tổng và kiểm tra quyền
        var session = await _uow.DiningSessions.GetByIdWithParticipantsAndOrdersAsync(request.SessionId)
            ?? throw new EntityNotFoundException("Dining Session", request.SessionId);

        // 2. Guard trùng thanh toán sớm — chặn nếu đã có payment PENDING/SUCCESS cho session
        var existing = await _uow.Payments.GetBySessionIdAsync(request.SessionId);
        if (existing != null)
        {
            if (existing.PaymentStatus == PaymentStatus.PENDING)
                throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_ALREADY_PENDING);
            if (existing.PaymentStatus == PaymentStatus.SUCCESS)
                throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_ALREADY_COMPLETED);
            // FAILED → cho phép tạo lại
        }

        // 3. Kiểm tra trạng thái session
        if (session.Status == DiningSessionStatus.CHECKOUT)
            throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_SESSION_CHECKOUT_IN_PROGRESS);
        if (session.Status != DiningSessionStatus.ACTIVE)
            throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_SESSION_CLOSED);

        // 4. Ownership check — STAFF bỏ qua; CUSTOMER/GUEST phải là participant đang active
        EnsureCallerCanPay(session.Participants, callerCustomerId, callerGuestSessionId, isStaff);

        // 5. Tính tổng hóa đơn — dùng snapshot thuế/phí của phiên (Manager đổi settings → phiên sau)
        var subTotal = session.Orders.Sum(o => o.FinalAmount);
        if (subTotal <= 0)
            throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_NO_ORDERS);

        var settings = await _uow.Settings.GetSingletonAsync();
        var (taxRate, serviceRate) = BillingCalculator.ResolveRates(
            session.TaxRate, session.ServiceChargeRate, settings.TaxRate, settings.ServiceChargeRate);
        var (_, _, totalPayable) = BillingCalculator.Compute(subTotal, taxRate, serviceRate);

        // 6. Parse payment method — unknown method fallback về VNPAY (không lỗi, tránh UX xấu)
        var paymentMethod = Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var pm)
            ? pm
            : PaymentMethod.VNPAY;

        // 7. Tạo orderCode từ PostgreSQL sequence (unique tuyệt đối) + mã hóa đơn hiển thị
        var orderCode = await _uow.Payments.GetNextOrderCodeAsync();
        var invoiceId = GenerateInvoiceId(orderCode);

        // 8. Gọi cổng thanh toán (CASH bỏ qua gateway)
        string? qrUrl = null;
        string? deeplink = null;
        string? externalRef = orderCode.ToString();

        if (paymentMethod != PaymentMethod.CASH)
        {
            var gatewayResult = await _gateway.CreatePaymentLinkAsync(
                orderCode: orderCode,
                amount: (int)totalPayable,
                description: $"Thanh toan {invoiceId}",
                returnUrl: PaymentReturnUrl,
                cancelUrl: PaymentCancelUrl);

            if (!gatewayResult.Success)
                throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_GATEWAY_ERROR);

            qrUrl = gatewayResult.QrCode;
            deeplink = gatewayResult.CheckoutUrl;
            externalRef = gatewayResult.OrderCode.ToString();
        }

        // 9. Lưu Payment record với trạng thái PENDING
        var payment = new Payment
        {
            SessionId = session.Id,
            InvoiceId = invoiceId,
            Amount = totalPayable,
            PaymentMethod = paymentMethod,
            PaymentStatus = PaymentStatus.PENDING,
            QrUrl = qrUrl,
            Deeplink = deeplink,
            ExternalRef = externalRef,
            SplitCount = request.SplitCount > 0 ? request.SplitCount : 1
        };
        await _uow.Payments.AddAsync(payment);

        // 10. Khóa session để không cho đặt thêm món trong khi đang thanh toán
        session.Status = DiningSessionStatus.CHECKOUT;
        await _uow.DiningSessions.UpdateAsync(session);

        await _uow.SaveChangesAsync();

        // Tiền mặt: báo staffboard tab Thanh toán & Hóa đơn để thu tiền tại quầy
        if (paymentMethod == PaymentMethod.CASH)
        {
            var tableNumber = session.Table?.TableNumber ?? 0;
            if (tableNumber == 0)
            {
                var table = await _uow.Tables.GetByIdAsync(session.TableId);
                tableNumber = table?.TableNumber ?? 0;
            }

            await _notificationService.NotifyCashPaymentPendingAsync(
                session.TableId,
                tableNumber,
                invoiceId,
                totalPayable);
        }

        return new CreatePaymentIntentResponse
        {
            InvoiceId = invoiceId,
            TotalPayable = totalPayable,
            QrUrl = qrUrl ?? deeplink,
            Deeplink = deeplink
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/payments/cancel-intent
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Khách chủ động hủy giao dịch đang chờ (đóng dialog QR mà không quét, đổi ý...).
    ///
    /// Trước đây không có API này: đóng dialog ở FE chỉ ẩn UI, payment vẫn PENDING và
    /// session vẫn khóa CHECKOUT tới khi PaymentExpiryJob tự dọn sau tối đa 30 phút —
    /// trong lúc đó khách không gọi thêm món/thanh toán lại được. Gọi API này để mở khóa
    /// ngay lập tức thay vì bắt khách chờ.
    ///
    /// Đánh Payment → FAILED (cho phép tạo lại ngay, theo đúng luồng CreateIntentAsync đã
    /// cho phép retry khi FAILED) + session → ACTIVE nếu đang CHECKOUT.
    ///
    /// Ai dùng: PaymentsController.CancelIntent — DINER, GUEST, STAFF (cùng quyền với create-intent).
    /// </summary>
    public async Task CancelIntentAsync(
        int? callerCustomerId,
        string? callerGuestSessionId,
        bool isStaff,
        int sessionId)
    {
        var session = await _uow.DiningSessions.GetByIdWithParticipantsAndOrdersAsync(sessionId)
            ?? throw new EntityNotFoundException("Dining Session", sessionId);

        EnsureCallerCanPay(session.Participants, callerCustomerId, callerGuestSessionId, isStaff);

        var existing = await _uow.Payments.GetBySessionIdAsync(sessionId);
        if (existing == null || existing.PaymentStatus != PaymentStatus.PENDING)
            throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_NO_PENDING_TO_CANCEL);

        existing.PaymentStatus = PaymentStatus.FAILED;
        await _uow.Payments.UpdateAsync(existing);

        if (session.Status == DiningSessionStatus.CHECKOUT)
        {
            session.Status = DiningSessionStatus.ACTIVE;
            await _uow.DiningSessions.UpdateAsync(session);
        }

        await _uow.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/payments/webhook
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Xử lý webhook từ PayOS gọi về sau khi giao dịch hoàn tất.
    ///
    /// Bước 1: Xác minh chữ ký HMAC-SHA256 — từ chối nếu sai (tránh giả mạo webhook).
    /// Bước 2: Tìm Payment theo ExternalRef (PayOS orderCode).
    /// Bước 3: Idempotency — nếu đã SUCCESS thì trả về luôn, không xử lý 2 lần.
    /// Bước 4: Thanh toán thành công → đóng session + giải phóng bàn + cộng điểm loyalty.
    /// Bước 5: Thanh toán thất bại → revert session về ACTIVE cho phép retry.
    ///
    /// Response luôn là {"RspCode":"00","Message":"..."} — quy định của PayOS webhook contract.
    /// Ai gọi: PayOS server (THIRD_PARTY_GATEWAY) — không qua JWT auth.
    /// </summary>
    public async Task<PaymentWebhookResponse> HandleWebhookAsync(string rawBody, string? signature)
    {
        // 1. Xác minh chữ ký
        var webhookData = _gateway.VerifyAndParseWebhook(rawBody, signature);
        if (webhookData == null)
            throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_WEBHOOK_INVALID_SIGNATURE);

        // 2. Tìm Payment theo ExternalRef
        var payment = await _uow.Payments.GetByExternalRefAsync(webhookData.OrderCode.ToString());
        if (payment == null)
        {
            if (webhookData.OrderCode == PayOsWebhookVerificationOrderCode)
                return new PaymentWebhookResponse { RspCode = "00", Message = "Webhook URL verified" };

            throw new EntityNotFoundException("Payment", webhookData.OrderCode.ToString());
        }

        // 3. Idempotency: đã xử lý thành công rồi → không làm gì thêm
        if (payment.PaymentStatus == PaymentStatus.SUCCESS)
            return new PaymentWebhookResponse { RspCode = "00", Message = "Already processed" };

        int? notifyTableId = null;
        int notifyTableNumber = 0;

        if (webhookData.IsSuccess)
        {
            // 4a. Thanh toán thành công
            payment.PaymentStatus = PaymentStatus.SUCCESS;
            payment.PaidAt = DateTime.UtcNow;
            await _uow.Payments.UpdateAsync(payment);

            var session = await _uow.DiningSessions.GetByIdWithParticipantsAndOrdersAsync(payment.SessionId)
                ?? throw new EntityNotFoundException("Dining Session", payment.SessionId);

            // Sau thanh toán: bàn sang MAINTENANCE để nhân viên dọn dẹp,
            // rồi staff chuyển lại AVAILABLE khi dọn xong.
            var table = await _uow.Tables.GetByIdAsync(session.TableId);
            if (table != null)
            {
                table.Status = TableStatus.MAINTENANCE;
                await _uow.Tables.UpdateAsync(table);
                notifyTableNumber = table.TableNumber;
            }

            notifyTableId = session.TableId;

            // Cộng điểm loyalty trước khi soft-delete session/orders
            await AwardLoyaltyPointsAsync(session, payment.Amount);

            await CloseAndSoftDeleteSessionAsync(session, payment.Amount);
        }
        else
        {
            // 4b. Thanh toán thất bại — revert session về ACTIVE để khách retry
            payment.PaymentStatus = PaymentStatus.FAILED;
            await _uow.Payments.UpdateAsync(payment);

            var session = await _uow.DiningSessions.GetByIdAsync(payment.SessionId);
            if (session != null && session.Status == DiningSessionStatus.CHECKOUT)
            {
                session.Status = DiningSessionStatus.ACTIVE;
                await _uow.DiningSessions.UpdateAsync(session);
            }
        }

        await _uow.SaveChangesAsync();

        // Thông báo realtime sau khi DB đã commit (dùng biến đã capture — session đã soft-delete)
        if (webhookData.IsSuccess && notifyTableId.HasValue)
        {
            await _notificationService.NotifyPaymentSuccessAsync(
                notifyTableId.Value,
                notifyTableNumber,
                payment.InvoiceId,
                payment.Amount);
        }

        return new PaymentWebhookResponse { RspCode = "00", Message = "Confirm success" };
    }

    // ═══════════════════════════════════════════════════════════════
    // Private helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Cộng điểm loyalty sau thanh toán thành công.
    /// amount_eligible = payment.Amount (đã trừ chiết khấu). earn_points = floor(amount_eligible / 1000).
    /// Chỉ áp dụng cho CUSTOMER (participant có CustomerId).
    ///
    /// Không xử lý tier ở đây — nâng/hạ hạng là một luồng riêng, xét trên TotalSpent tích lũy.
    ///
    /// Ai dùng: HandleWebhookAsync — nội bộ service.
    /// </summary>
    private async Task AwardLoyaltyPointsAsync(DiningSession session, decimal amount)
    {
        // Lấy CustomerId đầu tiên trong session (HOST nếu có, hoặc bất kỳ participant CUSTOMER nào)
        var customerId = session.Participants
            .Where(p => p.IsActive && p.CustomerId.HasValue)
            .OrderBy(p => p.JoinedAt)
            .FirstOrDefault()?.CustomerId;

        if (!customerId.HasValue) return; // GUEST session — không tích điểm

        var customer = await _uow.Customers.GetByIdAsync(customerId.Value);
        if (customer == null) return;

        var amountEligible = amount;
        var earnedPoints = (int)(amountEligible / 1000) * PointsPerThousandVnd;
        if (earnedPoints <= 0) return;

        customer.LoyaltyPoints += earnedPoints;
        customer.TotalSpent += amountEligible;
        customer.VisitCount += 1;

        await _uow.Customers.UpdateAsync(customer);

        await _uow.LoyaltyTransactions.AddAsync(new LoyaltyTransaction
        {
            CustomerId = customerId.Value,
            Points = earnedPoints,
            TransactionType = LoyaltyTransactionType.EARN
        });
    }

    /// <summary>
    /// Kiểm tra người gọi có quyền thanh toán cho session này không.
    /// STAFF: luôn được phép (đại diện thanh toán hộ khách).
    /// CUSTOMER/GUEST: phải là participant active của session.
    /// Cùng pattern với OrderService.EnsureCallerIsParticipant.
    /// </summary>
    private static void EnsureCallerCanPay(
        IEnumerable<SessionParticipant> participants,
        int? callerCustomerId,
        string? callerGuestSessionId,
        bool isStaff)
    {
        if (isStaff) return;

        var allowed = participants.Any(p =>
            p.IsActive &&
            ((callerCustomerId.HasValue && p.CustomerId == callerCustomerId) ||
             (!string.IsNullOrEmpty(callerGuestSessionId) && p.GuestSessionId == callerGuestSessionId)));

        if (!allowed)
            throw new UnauthorizedAccessException(ValidationMessages.PAYMENT_ACCESS_DENIED);
    }

    /// <summary>
    /// Tạo mã hóa đơn hiển thị cho khách: "INV-{năm}-{orderCode cuối 6 chữ số}".
    /// Ví dụ: "INV-2026-734812".
    /// </summary>
    private static string GenerateInvoiceId(long orderCode) =>
        $"INV-{DateTime.UtcNow:yyyy}-{orderCode % 1_000_000:D6}";

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/payments (Manager) — Lịch sử giao dịch
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy lịch sử giao dịch phân trang cho manager dashboard.
    /// Lọc theo khoảng ngày (CreatedAt), trạng thái, phương thức thanh toán.
    /// </summary>
    public async Task<(List<PaymentHistoryResponse> Items, int TotalCount, int TotalPages)> GetHistoryAsync(
        DateTime? fromDate, DateTime? toDate, string? status, string? paymentMethod, int page, int pageSize)
    {
        if (status != null && !Enum.TryParse<PaymentStatus>(status, true, out _))
            throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_STATUS_INVALID);

        if (paymentMethod != null && !Enum.TryParse<PaymentMethod>(paymentMethod, true, out _))
            throw new BusinessRuleViolationException(ValidationMessages.PAYMENT_METHOD_INVALID);

        var (items, totalCount) = await _uow.Payments.GetPagedFilteredAsync(
            fromDate, toDate, status, paymentMethod, page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return (items.Select(MapToHistoryResponse).ToList(), totalCount, totalPages);
    }

    /// <summary>
    /// Danh sách thanh toán tiền mặt đang PENDING — staffboard tab Thanh toán & Hóa đơn.
    /// </summary>
    public async Task<List<PendingCashPaymentResponse>> GetPendingCashAsync()
    {
        var payments = await _uow.Payments.GetPendingCashAsync();
        return payments.Select(p => new PendingCashPaymentResponse
        {
            PaymentId = p.Id,
            InvoiceId = p.InvoiceId,
            SessionId = p.SessionId,
            TableId = p.Session.TableId,
            TableNumber = p.Session.Table?.TableNumber ?? 0,
            Amount = p.Amount,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    private static PaymentHistoryResponse MapToHistoryResponse(Payment payment) => new()
    {
        Id = payment.Id,
        InvoiceId = payment.InvoiceId,
        SessionId = payment.SessionId,
        TableId = payment.Session.TableId,
        TableNumber = payment.Session.Table.TableNumber,
        CustomerName = payment.Session.Customer?.FullName ?? payment.Session.GuestName ?? "Guest",
        Amount = payment.Amount,
        PaymentMethod = payment.PaymentMethod.ToString(),
        PaymentStatus = payment.PaymentStatus.ToString(),
        ExternalRef = payment.ExternalRef,
        PaidAt = payment.PaidAt,
        CreatedAt = payment.CreatedAt
    };

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/payments/revenue-summary (Manager) — Doanh thu tổng quan cho dashboard
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Doanh thu hôm nay + tháng này — chỉ tính payment đã PaymentStatus = SUCCESS
    /// (GetByDateRangeAsync lọc theo PaidAt, field chỉ được set khi thanh toán thành công,
    /// nên PENDING/FAILED/EXPIRED tự động bị loại, không cần filter status thủ công).
    /// </summary>
    public async Task<RevenueSummaryResponse> GetRevenueSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthPayments = await _uow.Payments.GetByDateRangeAsync(monthStart, now);

        return new RevenueSummaryResponse
        {
            TodayRevenue = monthPayments.Where(p => p.PaidAt >= todayStart).Sum(p => p.Amount),
            MonthRevenue = monthPayments.Sum(p => p.Amount)
        };
    }

    /// <summary>
    /// Chart doanh thu thực nhận (chỉ payment SUCCESS) cho Dashboard Manager — "Actual Revenue".
    /// period: day (theo giờ hôm nay) | week (7 ngày) | month (từ đầu tháng).
    /// GetByDateRangeAsync lọc theo PaidAt (chỉ set khi SUCCESS) nên không cần lọc status thủ công.
    /// </summary>
    public async Task<List<ChartPointResponse>> GetRevenueChartAsync(string? period)
    {
        var (start, end) = ChartPeriodHelper.ResolveRange(period);
        var payments = await _uow.Payments.GetByDateRangeAsync(start, end);
        return ChartPeriodHelper.Bucket(payments.Select(p => (p.PaidAt!.Value, p.Amount)), period);
    }

    public async Task<bool> CompletePaymentAsync(int paymentId)
    {
        var payment = await _uow.Payments.GetByIdAsync(paymentId)
            ?? throw new EntityNotFoundException("Payment", paymentId);

        if (payment.PaymentStatus == PaymentStatus.SUCCESS)
            return true;

        payment.PaymentStatus = PaymentStatus.SUCCESS;
        payment.PaidAt = DateTime.UtcNow;
        await _uow.Payments.UpdateAsync(payment);

        var session = await _uow.DiningSessions.GetByIdWithParticipantsAndOrdersAsync(payment.SessionId)
            ?? throw new EntityNotFoundException("Dining Session", payment.SessionId);

        // MAINTENANCE: nhân viên dọn bàn rồi mới chuyển AVAILABLE trên Table Management
        var table = await _uow.Tables.GetByIdAsync(session.TableId);
        if (table != null)
        {
            table.Status = TableStatus.MAINTENANCE;
            await _uow.Tables.UpdateAsync(table);
        }

        await AwardLoyaltyPointsAsync(session, payment.Amount);
        await CloseAndSoftDeleteSessionAsync(session, payment.Amount);

        // Lưu DB trước — tránh SignalR lỗi làm mất cập nhật payment/session/table
        await _uow.SaveChangesAsync();

        await _notificationService.NotifyPaymentSuccessAsync(
            session.TableId, table?.TableNumber ?? 0, payment.InvoiceId, payment.Amount);

        return true;
    }

    /// <summary>
    /// Đóng phiên (CLOSED) + soft-delete lịch sử phiên (session, orders, order details).
    /// Payment giữ lại để manager xem doanh thu / lịch sử giao dịch.
    /// </summary>
    private async Task CloseAndSoftDeleteSessionAsync(DiningSession session, decimal totalSpent)
    {
        session.Status = DiningSessionStatus.CLOSED;
        session.EndedAt = DateTime.UtcNow;
        session.TotalSpent = totalSpent;
        session.IsDeleted = true;
        session.UpdatedAt = DateTime.UtcNow;

        if (session.Participants != null)
        {
            foreach (var participant in session.Participants.Where(p => p.LeftAt == null))
                participant.LeftAt = DateTime.UtcNow;
        }

        var orders = session.Orders?.ToList() ?? new List<Order>();
        if (orders.Count == 0)
            orders = (await _uow.Orders.GetByDiningSessionIdAsync(session.Id)).ToList();

        foreach (var order in orders)
        {
            foreach (var detail in order.OrderDetails.ToList())
            {
                detail.IsDeleted = true;
                detail.UpdatedAt = DateTime.UtcNow;
                await _uow.OrderDetails.UpdateAsync(detail);
            }

            order.IsDeleted = true;
            order.UpdatedAt = DateTime.UtcNow;
            await _uow.Orders.UpdateAsync(order);
        }

        await _uow.DiningSessions.UpdateAsync(session);
    }

    public async Task<bool> CompletePaymentByTableAsync(int tableNumber)
    {
        var tableObj = await _uow.Tables.GetByTableNumberAsync(tableNumber)
            ?? throw new EntityNotFoundException("Table", tableNumber);

        // ACTIVE (staff thu trước) hoặc CHECKOUT (khách đã tạo intent tiền mặt)
        var session = await _uow.DiningSessions.GetPayableByTableIdAsync(tableObj.Id)
            ?? throw new EntityNotFoundException("Active Dining Session for Table", tableNumber);

        var payment = await _uow.Payments.GetBySessionIdAsync(session.Id);
        if (payment == null)
        {
            var subTotal = session.Orders.Sum(o => o.FinalAmount);
            var settings = await _uow.Settings.GetSingletonAsync();
            var (taxRate, serviceRate) = BillingCalculator.ResolveRates(
                session.TaxRate, session.ServiceChargeRate, settings.TaxRate, settings.ServiceChargeRate);
            var (_, _, totalPayable) = BillingCalculator.Compute(subTotal, taxRate, serviceRate);
            var orderCode = await _uow.Payments.GetNextOrderCodeAsync();
            var invoiceId = GenerateInvoiceId(orderCode);

            payment = new Payment
            {
                SessionId = session.Id,
                InvoiceId = invoiceId,
                Amount = totalPayable,
                PaymentMethod = PaymentMethod.CASH,
                PaymentStatus = PaymentStatus.PENDING,
                ExternalRef = orderCode.ToString(),
                SplitCount = 1
            };
            await _uow.Payments.AddAsync(payment);
            await _uow.SaveChangesAsync();
        }

        return await CompletePaymentAsync(payment.Id);
    }
}
