namespace SmartDine.Domain.Entities;

/// <summary>
/// Chi tiết từng món trong đơn hàng.
/// </summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? SpecialInstructions { get; set; }
}
