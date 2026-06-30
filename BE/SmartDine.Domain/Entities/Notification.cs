using System;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Thông báo hệ thống (notifications).
/// </summary>
public class Notification : BaseEntity
{
    public UserType RecipientType { get; set; } = UserType.CUSTOMER;
    public int RecipientId { get; set; }
    public string NotificationType { get; set; } = string.Empty; // ORDER_CREATED, ORDER_READY, etc.
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; } // JSON format payload
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
