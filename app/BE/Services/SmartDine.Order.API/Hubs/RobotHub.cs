using Microsoft.AspNetCore.SignalR;

namespace SmartDine.Order.API.Hubs;

public class RobotHub : Hub
{
    private readonly ILogger<RobotHub> _logger;

    public RobotHub(ILogger<RobotHub> logger)
    {
        _logger = logger;
    }

    public async Task SendRobotCommand(string command, string target, string direction)
    {
        _logger.LogInformation("RobotCommand: {Command} | Target: {Target} | Direction: {Direction}",
            command, target, direction);

        await Clients.Group("RobotGroup").SendAsync("ReceiveRobotCommand", new
        {
            command,
            target,
            direction
        });
    }

    public async Task SendRobotState(double x, double y, double theta,
                                      double v, double omega, string status)
    {
        _logger.LogDebug("RobotState: ({X:F3},{Y:F3}) θ={Theta:F3} v={V:F3} ω={Omega:F3} status={Status}",
            x, y, theta, v, omega, status);

        await Clients.Group("RobotGroup").SendAsync("ReceiveRobotState", new
        {
            x, y, theta, v, omega, status
        });
    }

    public async Task SendRobotPath(List<PathPoint> path)
    {
        _logger.LogDebug("RobotPath: {PointCount} points", path.Count);

        await Clients.Group("RobotGroup").SendAsync("ReceiveRobotPath", new
        {
            path
        });
    }

    public async Task JoinRobotGroup()
    {
        _logger.LogInformation("RobotGroup JOIN: connection={ConnectionId}", Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, "RobotGroup");
    }

    public async Task LeaveRobotGroup()
    {
        _logger.LogInformation("RobotGroup LEAVE: connection={ConnectionId}", Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "RobotGroup");
    }
}

/// <summary>
/// DTO cho tọa độ điểm trên đường đi.
/// </summary>
public class PathPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}
