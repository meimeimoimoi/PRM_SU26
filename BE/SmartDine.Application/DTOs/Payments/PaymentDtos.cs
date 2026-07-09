namespace SmartDine.Application.DTOs.Payments;

// ───────────────────────── Request ─────────────────────────

/// <summary>
/// Yêu cầu khởi tạo phiên thanh toán cho 1 DiningSession.
/// Ai gửi: DINER / GUEST / STAFF qua POST /api/v1/payments/create-intent.
/// </summary>
public class CreatePaymentIntentRequest
{
    /// <summary>ID phiên ăn cần thanh toán.</summary>
    public int SessionId { get; set; }

    /// <summary>
    /// Phương thức thanh toán: "VNPAY", "MOMO", "QR", "CASH".
    /// Mặc định VNPAY nếu bỏ trống.
    /// </summary>
    public string PaymentMethod { get; set; } = "VNPAY";

    /// <summary>
    /// Số người chia hóa đơn. 1 = không chia.
    /// Hệ thống hiện tạo 1 link thanh toán duy nhất; split_count lưu lại để hiển thị.
    /// </summary>
    public int SplitCount { get; set; } = 1;
}

// ───────────────────────── Response ─────────────────────────

/// <summary>
/// Kết quả tạo phiên thanh toán — trả về cho client để hiển thị QR + deeplink.
/// </summary>
public class CreatePaymentIntentResponse
{
    /// <summary>Mã hóa đơn hiển thị: "INV-2026-XXXXXX".</summary>
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>Tổng tiền cần thanh toán (VND).</summary>
    public decimal TotalPayable { get; set; }

    /// <summary>URL ảnh VietQR hoặc trang checkout — client hiển thị dạng QR.</summary>
    public string? QrUrl { get; set; }

    /// <summary>Deeplink mở app thanh toán (VNPay/Momo).</summary>
    public string? Deeplink { get; set; }
}

// ─────────────────── Webhook (nhận từ PayOS) ────────────────

/// <summary>
/// Body webhook PayOS gọi về sau khi giao dịch hoàn tất.
/// Không expose ra ngoài — chỉ dùng internal trong PaymentsController.
/// </summary>
public class PayOsWebhookRequest
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public bool Success { get; set; }
    public PayOsWebhookData? Data { get; set; }
    public string? Signature { get; set; }
}

public class PayOsWebhookData
{
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? Reference { get; set; }
    public string? TransactionDateTime { get; set; }
    public string? PaymentLinkId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
}

/// <summary>Response chuẩn PayOS webhook: {"RspCode":"00","Message":"Confirm success"}.</summary>
public class PaymentWebhookResponse
{
    public string RspCode { get; set; } = "00";
    public string Message { get; set; } = "Confirm success";
}
