using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Lịch sử tích điểm / đổi điểm thành viên (loyalty_transactions).
/// Tạo 1 record EARN sau mỗi lần thanh toán thành công (1 điểm / 1.000 VND).
/// Tạo 1 record REDEEM khi khách dùng điểm đổi ưu đãi (chức năng tương lai).
/// </summary>
public class LoyaltyTransaction : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>FK đến Order — nullable vì REDEEM có thể không gắn với order cụ thể.</summary>
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    public int Points { get; set; }

    /// <summary>Lưu dưới dạng string trong DB ("EARN"/"REDEEM") qua EF HasConversion.</summary>
    public LoyaltyTransactionType TransactionType { get; set; } = LoyaltyTransactionType.EARN;
}
