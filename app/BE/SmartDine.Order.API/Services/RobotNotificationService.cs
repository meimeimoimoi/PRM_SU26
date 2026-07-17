using Microsoft.AspNetCore.SignalR;
using SmartDine.Order.API.Hubs;

namespace SmartDine.Order.API.Services;

/// <summary>
/// Service gửi tin nhắn robot real-time qua SignalR RobotHub.
/// Dashboard gọi từ service này để gửi command đến robot.
/// Sidecar gọi từ service này để push state/path lên Dashboard.
/// </summary>
public class RobotNotificationService
{
    private readonly IHubContext<RobotHub> _hubContext;

    public RobotNotificationService(IHubContext<RobotHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Gửi command đến robot group (Dashboard → Backend → Sidecar).
    /// </summary>
    public async Task SendCommandToRobotAsync(string command, string target, string direction)
    {
        await _hubContext.Clients.Group("RobotGroup").SendAsync("ReceiveRobotCommand", new
        {
            command,
            target,
            direction
        });
    }

    /// <summary>
    /// Broadcast trạng thái robot lên tất cả client trong RobotGroup.
    /// </summary>
    public async Task BroadcastRobotStateAsync(
        double x, double y, double theta,
        double v, double omega, string status)
    {
        await _hubContext.Clients.Group("RobotGroup").SendAsync("ReceiveRobotState", new
        {
            x, y, theta, v, omega, status
        });
    }

    /// <summary>
    /// Broadcast đường đi robot lên tất cả client trong RobotGroup.
    /// </summary>
    public async Task BroadcastRobotPathAsync(List<PathPoint> path)
    {
        await _hubContext.Clients.Group("RobotGroup").SendAsync("ReceiveRobotPath", new
        {
            path
        });
    }

    /// <summary>
    /// Gửi thông báo robot đã hoàn thành giao hàng.
    /// </summary>
    public async Task NotifyDeliveryCompletedAsync(string robotCode, int tableNumber)
    {
        await _hubContext.Clients.Group("RobotGroup").SendAsync("DeliveryCompleted", new
        {
            robotCode,
            tableNumber,
            timestamp = DateTime.UtcNow
        });
    }
}
