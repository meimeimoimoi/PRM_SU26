using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Đơn hàng của khách - Entity thuần, không phụ thuộc thư viện nào, chưa phải entity thật.
/// </summary>
public class Order
{
    public Guid Id { get; set; }
    public List<MenuItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
