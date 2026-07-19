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

    /// <summary>
    /// Snapshot thuế/phí tại lúc mở phiên. Manager đổi settings chỉ áp dụng phiên sau.
    /// Null = phiên cũ trước khi có snapshot → fallback RestaurantSettings lúc tính bill.
    /// </summary>
    public decimal? TaxRate { get; set; }
    public decimal? ServiceChargeRate { get; set; }

    // Navigation
    public List<Order> Orders { get; set; } = new();
    public List<SessionParticipant> Participants { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
}
