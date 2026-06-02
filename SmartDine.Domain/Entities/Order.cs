using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Đơn hàng của khách — Aggregate Root.
/// Chứa business rules cho status transitions.
/// </summary>
public class Order : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid TableId { get; set; }
    public Table Table { get; set; } = null!;

    public Guid? DiningSessionId { get; set; }
    public DiningSession? DiningSession { get; set; }

    public Guid? PromotionId { get; set; }
    public Promotion? Promotion { get; set; }

    public List<OrderItem> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PENDING;
    public string? SpecialInstructions { get; set; }

    // Navigation
    public Payment? Payment { get; set; }

    /// <summary>
    /// Tính tổng tiền dựa trên các items.
    /// </summary>
    public void CalculateTotal()
    {
        SubTotal = Items.Sum(i => i.Quantity * i.UnitPrice);
        TotalAmount = SubTotal - DiscountAmount;
        if (TotalAmount < 0) TotalAmount = 0;
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng với validation business rules.
    /// </summary>
    public void UpdateStatus(OrderStatus newStatus)
    {
        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            { OrderStatus.PENDING,   new[] { OrderStatus.CONFIRMED, OrderStatus.CANCELLED } },
            { OrderStatus.CONFIRMED, new[] { OrderStatus.COOKING, OrderStatus.CANCELLED } },
            { OrderStatus.COOKING,   new[] { OrderStatus.READY } },
            { OrderStatus.READY,     new[] { OrderStatus.COMPLETED } },
        };

        if (!validTransitions.ContainsKey(Status) ||
            !validTransitions[Status].Contains(newStatus))
        {
            throw new BusinessRuleViolationException(
                $"Cannot transition order from {Status} to {newStatus}");
        }

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
