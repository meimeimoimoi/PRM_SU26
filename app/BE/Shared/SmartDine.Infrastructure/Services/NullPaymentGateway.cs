using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Services;

/// <summary>
/// Fallback no-op implementation của IPaymentGateway — dùng cho integration tests và monolith.
/// Order.API ghi đè bằng PayOsGateway thông qua AddHttpClient&lt;IPaymentGateway, PayOsGateway&gt;().
/// </summary>
public class NullPaymentGateway : IPaymentGateway
{
    public Task<GatewayCreatePaymentResult> CreatePaymentLinkAsync(
        long orderCode, int amount, string description, string returnUrl, string cancelUrl) =>
        Task.FromResult(new GatewayCreatePaymentResult(
            Success: false,
            CheckoutUrl: null,
            QrCode: null,
            PaymentLinkId: null,
            OrderCode: orderCode,
            ErrorMessage: "PaymentGateway không được cấu hình trong môi trường này."));

    public GatewayWebhookData? VerifyAndParseWebhook(string webhookBody, string? signature) => null;
}
