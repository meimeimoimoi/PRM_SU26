namespace SmartDine.Application.DTOs.Payments;

public class CreatePaymentIntentRequest
{
    public int SessionId { get; set; }
    public string PaymentMethod { get; set; } = "VNPAY";
    public int SplitCount { get; set; } = 1;
}

public class CreatePaymentIntentResponse
{
    public string InvoiceId { get; set; } = string.Empty;
    public decimal TotalPayable { get; set; }
    public string? QrUrl { get; set; }
    public string? Deeplink { get; set; }
}

public class PaymentWebhookResponse
{
    public string RspCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
