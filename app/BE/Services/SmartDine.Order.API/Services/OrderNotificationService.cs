using Microsoft.AspNetCore.SignalR;
using SmartDine.Order.API.Hubs;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Order.API.Services;

/// <summary>
/// Thực thi IOrderNotificationService gửi tin nhắn qua SignalR Hub.
/// </summary>
public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrderHub> _hubContext;

    public OrderNotificationService(IHubContext<OrderHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewOrderAsync(int orderId, int tableNumber, decimal totalAmount)
    {
        // Gửi thông báo đến nhóm "KitchenGroup" cho bếp và nhân viên phục vụ biết
        await _hubContext.Clients.Group("KitchenGroup").SendAsync("ReceiveNewOrder", new
        {
            OrderId = orderId,
            TableNumber = tableNumber,
            TotalAmount = totalAmount,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task NotifyOrderStatusChangedAsync(int orderId, int tableId, string status)
    {
        // Gửi thông báo đến nhóm khách hàng đang ngồi tại bàn "table_{tableId}"
        await _hubContext.Clients.Group($"table_{tableId}").SendAsync("ReceiveOrderStatusUpdate", new
        {
            OrderId = orderId,
            TableId = tableId,
            Status = status,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Thông báo thanh toán thành công.
    /// Client tại bàn lắng nghe event "ReceivePaymentSuccess" để tự ẩn QR và hiển thị màn hình
    /// "Cảm ơn". Đồng thời gửi tới "KitchenGroup" để chuông thông báo trên web-dashboard
    /// (STAFF/MANAGER) nhận được ngay, không cần F5.
    /// </summary>
    public async Task NotifyPaymentSuccessAsync(int tableId, int tableNumber, string invoiceId, decimal amount)
    {
        var payload = new
        {
            TableId = tableId,
            TableNumber = tableNumber,
            InvoiceId = invoiceId,
            Amount = amount,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"table_{tableId}").SendAsync("ReceivePaymentSuccess", payload);
        await _hubContext.Clients.Group("KitchenGroup").SendAsync("ReceivePaymentSuccess", payload);
    }
}
