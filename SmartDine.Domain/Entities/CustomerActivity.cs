namespace SmartDine.Domain.Entities;

/// <summary>
/// Ghi lại hoạt động của khách hàng phục vụ cho AI recommendations.
/// </summary>
public class CustomerActivity : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string ActivityType { get; set; } = string.Empty; // VIEW, ORDER, REVIEW, SEARCH
    public Guid? MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public string? Metadata { get; set; } // JSON for flexible data
}
