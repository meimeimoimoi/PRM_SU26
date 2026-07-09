namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Kết quả tạo link thanh toán từ cổng bên thứ 3.
/// </summary>
/// <param name="Success">True nếu cổng trả về thành công (code "00").</param>
/// <param name="CheckoutUrl">URL trang thanh toán (deeplink / web checkout).</param>
/// <param name="QrCode">Chuỗi QR raw (EMV/VietQR) — client render thành ảnh.</param>
/// <param name="PaymentLinkId">ID link thanh toán phía cổng (dùng để cancel).</param>
/// <param name="OrderCode">Mã đơn số của cổng — key khớp với webhook gọi về.</param>
/// <param name="ErrorMessage">Thông báo lỗi nếu Success = false.</param>
public record GatewayCreatePaymentResult(
    bool Success,
    string? CheckoutUrl,
    string? QrCode,
    string? PaymentLinkId,
    long OrderCode,
    string? ErrorMessage = null
);

/// <summary>
/// Dữ liệu đã xác minh từ webhook cổng thanh toán.
/// </summary>
/// <param name="OrderCode">Mã đơn số — khớp với ExternalRef trong bảng payments.</param>
/// <param name="Amount">Số tiền (VND).</param>
/// <param name="Description">Mô tả giao dịch.</param>
/// <param name="IsSuccess">True nếu thanh toán thành công.</param>
public record GatewayWebhookData(
    long OrderCode,
    int Amount,
    string Description,
    bool IsSuccess
);

/// <summary>
/// Abstraction cổng thanh toán bên thứ 3.
/// Implement: PayOsGateway (Infrastructure). Mock: dùng trong unit tests.
///
/// Ai dùng: PaymentService (Application layer).
/// Lý do tách interface: giữ Application không phụ thuộc vào SDK cụ thể (PayOS/VNPay/Momo).
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Gọi PayOS API tạo link thanh toán.
    /// Trả về CheckoutUrl + QrCode dùng để hiển thị cho khách.
    /// </summary>
    Task<GatewayCreatePaymentResult> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        string returnUrl,
        string cancelUrl);

    /// <summary>
    /// Xác minh chữ ký HMAC-SHA256 của webhook và parse dữ liệu.
    /// Trả về null nếu chữ ký không hợp lệ — caller phải từ chối request.
    /// </summary>
    GatewayWebhookData? VerifyAndParseWebhook(string webhookBody, string? signature);
}
