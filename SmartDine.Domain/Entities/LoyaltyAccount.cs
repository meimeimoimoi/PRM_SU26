using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Tài khoản loyalty của khách hàng — tích điểm, đổi thưởng.
/// </summary>
public class LoyaltyAccount : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int TotalPoints { get; set; }
    public int CurrentPoints { get; set; }
    public LoyaltyTier Tier { get; set; } = LoyaltyTier.BRONZE;

    // Navigation
    public List<LoyaltyTransaction> Transactions { get; set; } = new();
}
