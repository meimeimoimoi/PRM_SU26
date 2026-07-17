using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartDine.Order.API.Hubs;

/// <summary>
/// Hub SignalR cho giao tiếp real-time với Robot sidecar.
/// Dashboard gửi commands, Sidecar push state/path.
/// </summary>
[Authorize]
public class RobotHub : Hub
{
    /// <summary>
    /// Dashboard gửi command đến robot (NAV_TO_TABLE, STOP, CALIBRATE, MANUAL_MOVE).
    /// </summary>
    public async Task SendRobotCommand(string command, string target, string direction)
    {
        await Clients.Group("RobotGroup").SendAsync("ReceiveRobotCommand", new
        {
            command,
            target,
            direction
        });
    }

    /// <summary>
    /// Sidecar push trạng thái robot lên server.
    /// </summary>
    public async Task SendRobotState(double x, double y, double theta,
                                      double v, double omega, string status)
    {
        await Clients.Group("RobotGroup").SendAsync("ReceiveRobotState", new
        {
            x, y, theta, v, omega, status
        });
    }

    /// <summary>
    /// Sidecar push đường đi robot lên server.
    /// </summary>
    public async Task SendRobotPath(List<PathPoint> path)
    {
        await Clients.Group("RobotGroup").SendAsync("ReceiveRobotPath", new
        {
            path
        });
    }

    /// <summary>
    /// Sidecar hoặc Dashboard tham gia nhóm RobotGroup.
    /// </summary>
    public async Task JoinRobotGroup()
        => await Groups.AddToGroupAsync(Context.ConnectionId, "RobotGroup");

    /// <summary>
    /// Rời khỏi nhóm RobotGroup.
    /// </summary>
    public async Task LeaveRobotGroup()
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "RobotGroup");
}

/// <summary>
/// DTO cho tọa độ điểm trên đường đi.
/// </summary>
public class PathPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}
