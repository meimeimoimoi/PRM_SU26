using System;
using System.Collections.Generic;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Phiên ăn uống (dining_sessions).
/// </summary>
public class DiningSession : BaseEntity
{
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int TableId { get; set; }
    public Table Table { get; set; } = null!;

    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public DiningSessionStatus Status { get; set; } = DiningSessionStatus.ACTIVE;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public decimal? TotalSpent { get; set; }

    // Navigation
    public List<Order> Orders { get; set; } = new();
    public List<SessionParticipant> Participants { get; set; } = new();
}
