using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Thông báo hệ thống cho người dùng.
/// </summary>
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.SYSTEM;
    public bool IsRead { get; set; } = false;
    public string? Data { get; set; } // JSON metadata
}
