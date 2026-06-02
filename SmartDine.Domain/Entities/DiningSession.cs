namespace SmartDine.Domain.Entities;

/// <summary>
/// Phiên ăn uống — từ khi khách ngồi xuống đến khi thanh toán xong.
/// Một session có thể có nhiều orders (gọi thêm món).
/// </summary>
public class DiningSession : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid TableId { get; set; }
    public Table Table { get; set; } = null!;

    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal TotalAmount { get; set; }
    public int GuestCount { get; set; } = 1;

    // Navigation
    public List<Order> Orders { get; set; } = new();
}
