using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Bản ghi thanh toán.
/// </summary>
public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.CASH;
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
    public string? TransactionRef { get; set; }
    public DateTime? PaidAt { get; set; }
}
