using System;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Hóa đơn thanh toán (payments).
/// </summary>
public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = Enums.PaymentMethod.CASH;
    public PaymentStatus PaymentStatus { get; set; } = Enums.PaymentStatus.SUCCESS;
    public DateTime? PaidAt { get; set; }
}
