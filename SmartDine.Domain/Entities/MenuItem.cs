namespace SmartDine.Domain.Entities;

/// <summary>
/// Món ăn trong thực đơn nhà hàng.
/// </summary>
public class MenuItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }

    public Guid CategoryId { get; set; }
    public MenuCategory Category { get; set; } = null!;

    public bool IsAvailable { get; set; } = true;
    public int PrepTimeMinutes { get; set; } = 15;
    public int? Calories { get; set; }
    public string? Allergens { get; set; } // JSON array stored as string

    // Navigation
    public List<OrderItem> OrderItems { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
}
