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
    /// Thông báo thanh toán thành công — gửi tới 2 nhóm:
    ///   "table_{tableId}": khách hàng tại bàn tự refresh UI, không cần F5.
    ///   "KitchenGroup": staff/manager dùng cho chuông thông báo trên web-dashboard.
    /// Event name: "ReceivePaymentSuccess".
    /// Ai dùng: PaymentService.HandleWebhookAsync/CompletePaymentAsync sau khi xác nhận SUCCESS.
    /// </summary>
    Task NotifyPaymentSuccessAsync(int tableId, int tableNumber, string invoiceId, decimal amount);
}
