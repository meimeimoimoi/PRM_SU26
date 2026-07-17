using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SmartDine.Domain.Constants;

namespace SmartDine.Order.API.Hubs;

/// <summary>
/// Hub SignalR cho phép truyền dữ liệu thời gian thực giữa Khách hàng, Nhân viên và Nhà bếp.
/// </summary>
[Authorize]
public class OrderHub : Hub
{
    /// <summary>
    /// Tham gia vào nhóm theo dõi bàn ăn cụ thể.
    /// </summary>
    public async Task JoinTableGroup(string tableId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"table_{tableId}");
    }

    /// <summary>
    /// Rời khỏi nhóm bàn ăn.
    /// </summary>
    public async Task LeaveTableGroup(string tableId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"table_{tableId}");
    }

    /// <summary>
    /// Tham gia nhóm nhà bếp (dành cho bếp và nhân viên).
    /// </summary>
    [Authorize(Roles = Roles.KitchenStaff)]
    public async Task JoinKitchenGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "KitchenGroup");
    }

    /// <summary>
    /// Rời khỏi nhóm nhà bếp.
    /// </summary>
    [Authorize(Roles = Roles.KitchenStaff)]
    public async Task LeaveKitchenGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "KitchenGroup");
    }
}
