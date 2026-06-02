using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Bàn ăn trong nhà hàng.
/// </summary>
public class Table : BaseEntity
{
    public int TableNumber { get; set; }
    public int Capacity { get; set; } = 4;
    public TableStatus Status { get; set; } = TableStatus.AVAILABLE;
    public string? QrCode { get; set; }
    public string? Location { get; set; } // e.g., "Indoor", "Outdoor", "VIP"

    // Navigation
    public List<DiningSession> DiningSessions { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}
