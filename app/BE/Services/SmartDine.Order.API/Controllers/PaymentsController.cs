using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Payments;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;
using SmartDine.Domain.Enums;

namespace SmartDine.Order.API.Controllers;

/// <summary>
/// Controller xử lý thanh toán hóa đơn phiên ăn.
///
/// Luồng:
///   DINER/GUEST/STAFF → POST create-intent → nhận QR/deeplink → khách quét / mở app → thanh toán
///   → PayOS gọi POST webhook → hệ thống đóng session + giải phóng bàn + cộng điểm.
///
/// Middleware:
///   IdempotencyMiddleware: chặn double-click dựa trên header "Idempotency-Key".
///   ExceptionHandlingMiddleware: map exception sang HTTP code phù hợp (422, 403, 404...).
/// </summary>
[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentsController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/payments
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lịch sử giao dịch phân trang cho manager dashboard.
    /// Lọc theo khoảng ngày tạo, trạng thái, phương thức thanh toán.
    /// Roles: MANAGER.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? status,
        [FromQuery] string? paymentMethod,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, total, totalPages) = await _paymentService.GetHistoryAsync(
            fromDate, toDate, status, paymentMethod, page, pageSize);
        return Ok(PaginatedApiResponse<PaymentHistoryResponse>.Ok(items, total, page, totalPages));
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/payments/create-intent
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo hóa đơn và link thanh toán VietQR / VNPay / Momo.
    ///
    /// Hệ thống sẽ:
    ///   1. Tính tổng tiền tất cả Order trong session.
    ///   2. Gọi PayOS API sinh mã QR động tích hợp số tiền.
    ///   3. Đặt session.Status = CHECKOUT (khóa không cho đặt thêm món).
    ///   4. Trả về invoice_id + qr_url + deeplink.
    ///
    /// Header tùy chọn: "Idempotency-Key: {uuid}" để chặn double request.
    /// Roles: DINER, GUEST, STAFF.
    /// </summary>
    [HttpPost("create-intent")]
    [Authorize(Roles = Roles.AllExceptChef)]
    public async Task<IActionResult> CreateIntent([FromBody] CreatePaymentIntentRequest request)
    {
        var (customerId, guestSessionId) = ExtractIdentity();
        var isStaff = IsStaff();

        var result = await _paymentService.CreateIntentAsync(customerId, guestSessionId, isStaff, request);
        return Ok(ApiResponse<CreatePaymentIntentResponse>.Ok(result, ValidationMessages.PAYMENT_INTENT_CREATED));
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/payments/webhook
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Endpoint nhận kết quả thanh toán từ PayOS (IPN/Webhook).
    ///
    /// KHÔNG yêu cầu JWT — PayOS gọi server-to-server, không có token người dùng.
    /// Bảo mật: xác minh chữ ký HMAC-SHA256 trong PaymentService trước khi xử lý.
    ///
    /// Sau khi xác nhận thành công:
    ///   - payment.Status → SUCCESS
    ///   - session.Status → CLOSED
    ///   - table.Status → AVAILABLE
    ///   - Cộng điểm loyalty cho CUSTOMER (nếu có)
    ///
    /// Response phải là {"RspCode":"00","Message":"Confirm success"} theo quy định PayOS.
    /// Ai gọi: PayOS server (THIRD_PARTY_GATEWAY).
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        // Đọc raw body để verify chữ ký (không dùng [FromBody] vì cần string gốc)
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync();

        // PayOS có thể gửi signature qua header hoặc trong body
        var signature = Request.Headers["x-payos-signature"].FirstOrDefault();

        var result = await _paymentService.HandleWebhookAsync(rawBody, signature);
        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════
    // Private helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tách định danh caller từ JWT claims.
    /// CUSTOMER: sub = int customerId.
    /// GUEST: sub = UUID string (guestSessionId).
    /// STAFF/other: cả hai null → PaymentService sẽ dùng isStaff flag.
    /// </summary>
    private (int? CustomerId, string? GuestSessionId) ExtractIdentity()
    {
        var role = User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirst("role")?.Value ?? "";
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        if (role == nameof(UserRole.CUSTOMER) && int.TryParse(sub, out var cid))
            return (cid, null);
        if (role == nameof(UserRole.GUEST))
            return (null, sub);

        return (null, null);
    }

    /// <summary>STAFF, CHEF, MANAGER đều được coi là staff — bypass ownership check.</summary>
    private bool IsStaff() =>
        User.IsInRole(nameof(UserRole.STAFF)) ||
        User.IsInRole(nameof(UserRole.CHEF)) ||
        User.IsInRole(nameof(UserRole.MANAGER));
}
