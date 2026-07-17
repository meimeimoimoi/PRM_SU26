using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Hóa đơn thanh toán (payments).
///
/// Một Payment đại diện cho 1 lần thanh toán của toàn bộ phiên ăn (DiningSession),
/// bao gồm tất cả các Order trong phiên đó. Không ánh xạ 1-1 với Order vì
/// thực tế nhà hàng: khách gọi nhiều lần → cuối bữa thanh toán 1 lần.
///
/// Luồng sống:
///   PENDING → SUCCESS (webhook confirm) / FAILED (webhook fail hoặc timeout).
/// Khi SUCCESS: session.Status → CLOSED, table.Status → AVAILABLE.
/// Khi FAILED: session.Status → ACTIVE (cho phép retry thanh toán).
///
/// ExternalRef lưu orderCode từ PayOS — key duy nhất để match webhook.
/// </summary>
public class Payment : BaseEntity
{
    /// <summary>FK đến DiningSession — thanh toán theo phiên ăn, không phải theo từng Order.</summary>
    public int SessionId { get; set; }
    public DiningSession Session { get; set; } = null!;

    /// <summary>FK đến Order (nullable) — giữ lại cho backward-compat với code cũ.</summary>
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    /// <summary>Mã hóa đơn hiển thị cho khách: "INV-2026-XXXXXX".</summary>
    public string InvoiceId { get; set; } = string.Empty;

    /// <summary>Tổng tiền cần thanh toán (sau chiết khấu nếu có).</summary>
    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.VNPAY;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.PENDING;

    /// <summary>URL ảnh VietQR hoặc raw QR string từ cổng thanh toán.</summary>
    public string? QrUrl { get; set; }

    /// <summary>Deeplink mở app thanh toán (VNPay, Momo, ...).</summary>
    public string? Deeplink { get; set; }

    /// <summary>
    /// Mã tham chiếu phía cổng thanh toán (PayOS orderCode).
    /// Dùng để match webhook gọi về — phải unique per payment.
    /// </summary>
    public string? ExternalRef { get; set; }

    /// <summary>Số người chia hóa đơn (mặc định 1 = không chia).</summary>
    public int SplitCount { get; set; } = 1;

    public DateTime? PaidAt { get; set; }
}
