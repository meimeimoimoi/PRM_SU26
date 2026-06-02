namespace SmartDine.Domain.Entities;

/// <summary>
/// Đánh giá và nhận xét của khách hàng.
/// </summary>
public class Review : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public string? ManagerReply { get; set; }
}
