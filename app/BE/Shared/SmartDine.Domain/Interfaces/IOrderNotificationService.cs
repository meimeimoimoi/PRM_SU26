namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Interface gửi thông báo thời gian thực đến khách hàng và nhà bếp (Clean Architecture).
/// </summary>
public interface IOrderNotificationService
{
    /// <summary>Thông báo cho nhà bếp khi có đơn đặt món mới.</summary>
    Task NotifyNewOrderAsync(int orderId, int tableNumber, decimal totalAmount);

    /// <summary>Thông báo cho bàn ăn/khách hàng khi trạng thái đơn hàng thay đổi.</summary>
    Task NotifyOrderStatusChangedAsync(int orderId, int tableId, string status);

    /// <summary>
    /// Thông báo thanh toán thành công đến bàn ăn — client tự refresh UI, không cần F5.
    /// Group nhận: "table_{tableId}" (cùng pattern với JoinTableGroup trên OrderHub).
    /// Event name: "ReceivePaymentSuccess".
    /// Ai dùng: PaymentService.HandleWebhookAsync sau khi xác nhận SUCCESS từ PayOS.
    /// </summary>
    Task NotifyPaymentSuccessAsync(int tableId, string invoiceId, decimal amount);
}
