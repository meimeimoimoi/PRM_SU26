namespace SmartDine.Domain.Entities;

/// <summary>
/// Danh mục món ăn: Khai vị, Món chính, Tráng miệng, Đồ uống, v.v.
/// </summary>
public class MenuCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string? IconUrl { get; set; }

    // Navigation
    public List<MenuItem> MenuItems { get; set; } = new();
}
