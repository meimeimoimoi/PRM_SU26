using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Chương trình khuyến mãi / mã giảm giá.
/// </summary>
public class Promotion : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PromotionType Type { get; set; } = PromotionType.PERCENTAGE;
    public decimal DiscountValue { get; set; } // Percent or fixed amount
    public decimal? MinOrderAmount { get; set; }
    public int MaxUses { get; set; } = 100;
    public int CurrentUses { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public List<Order> Orders { get; set; } = new();
}
