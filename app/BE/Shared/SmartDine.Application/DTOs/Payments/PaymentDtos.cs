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

/// <summary>
/// Response cho lịch sử giao dịch (manager dashboard).
/// </summary>
public class PaymentHistoryResponse
{
    public int Id { get; set; }
    public string InvoiceId { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public int TableId { get; set; }
    public int TableNumber { get; set; }
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? ExternalRef { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
