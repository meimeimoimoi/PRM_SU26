using System;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Thành viên tham gia phiên ăn uống (session_participants).
/// </summary>
public class SessionParticipant : BaseEntity
{
    public int SessionId { get; set; }
    public DiningSession Session { get; set; } = null!;

    // null khi là GUEST (không có tài khoản)
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // JWT sub claim của GUEST (dùng để identify khách vãng lai)
    public string? GuestSessionId { get; set; }

    public ParticipantRole Role { get; set; } = ParticipantRole.MEMBER;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }

    public bool IsActive => LeftAt == null;
}
