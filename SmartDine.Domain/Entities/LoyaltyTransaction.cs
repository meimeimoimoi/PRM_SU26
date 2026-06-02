namespace SmartDine.Domain.Entities;

/// <summary>
/// Lịch sử giao dịch điểm loyalty (earn/redeem).
/// </summary>
public class LoyaltyTransaction : BaseEntity
{
    public Guid LoyaltyAccountId { get; set; }
    public LoyaltyAccount LoyaltyAccount { get; set; } = null!;

    public int Points { get; set; } // Positive = earn, Negative = redeem
    public string Type { get; set; } = "EARN"; // EARN, REDEEM
    public string? Description { get; set; }

    public Guid? OrderId { get; set; }
}
