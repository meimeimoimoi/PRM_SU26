using Microsoft.AspNetCore.SignalR;
using SmartDine.Order.API.Hubs;

namespace SmartDine.Order.API.Services;

public class RobotNotificationService
{
    private readonly IHubContext<RobotHub> _hubContext;
    private readonly ILogger<RobotNotificationService> _logger;

    public RobotNotificationService(IHubContext<RobotHub> hubContext, ILogger<RobotNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendCommandToRobotAsync(string command, string target, string direction)
    {
        _logger.LogInformation("Service→Robot: {Command} | Target: {Target} | Direction: {Direction}",
            command, target, direction);

        await _hubContext.Clients.Group("RobotGroup").SendAsync("ReceiveRobotCommand", new
        {
            command,
            target,
            direction
        });
    }

    public async Task BroadcastRobotStateAsync(
        double x, double y, double theta,
        double v, double omega, string status)
    {
        _logger.LogDebug("Service→BroadcastState: ({X:F3},{Y:F3}) θ={Theta:F3} status={Status}",
            x, y, theta, status);

        await _hubContext.Clients.Group("RobotGroup").SendAsync("ReceiveRobotState", new
        {
            x, y, theta, v, omega, status
        });
    }

    public async Task BroadcastRobotPathAsync(List<PathPoint> path)
    {
        _logger.LogDebug("Service→BroadcastPath: {PointCount} points", path.Count);

        await _hubContext.Clients.Group("RobotGroup").SendAsync("ReceiveRobotPath", new
        {
            path
        });
    }

    public async Task NotifyDeliveryCompletedAsync(string robotCode, int tableNumber)
    {
        _logger.LogInformation("Service→DeliveryCompleted: Robot={RobotCode} Table={TableNumber}",
            robotCode, tableNumber);

        await _hubContext.Clients.Group("RobotGroup").SendAsync("DeliveryCompleted", new
        {
            robotCode,
            tableNumber,
            timestamp = DateTime.UtcNow
        });
    }
}
