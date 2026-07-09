using System;
using System.Collections.Generic;
using System.Linq;
using SmartDine.Domain.Constants;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Đơn hàng (orders).
/// </summary>
public class Order : BaseEntity
{
    public int SessionId { get; set; }
    public DiningSession Session { get; set; } = null!;

    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0.00m;
    public decimal FinalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PENDING;

    // Navigation
    public List<OrderDetail> OrderDetails { get; set; } = new();
    public List<Payment> Payments { get; set; } = new();
    public List<OrderPromotion> OrderPromotions { get; set; } = new();
    public List<OrderCombo> OrderCombos { get; set; } = new();

    /// <summary>
    /// Tính tổng tiền dựa trên các items.
    /// </summary>
    public void CalculateTotal()
    {
        TotalAmount = OrderDetails.Sum(i => i.Quantity * i.UnitPrice);
        FinalAmount = TotalAmount - DiscountAmount;
        if (FinalAmount < 0) FinalAmount = 0;
    }

    /// <summary>
    /// Cập nhật trạng thái với business validation.
    /// </summary>
    public void UpdateStatus(OrderStatus newStatus)
    {
        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            { OrderStatus.PENDING,   new[] { OrderStatus.COOKING, OrderStatus.COMPLETED, OrderStatus.CANCELLED } },
            { OrderStatus.COOKING,   new[] { OrderStatus.COMPLETED, OrderStatus.CANCELLED } },
            { OrderStatus.COMPLETED, Array.Empty<OrderStatus>() },
            { OrderStatus.CANCELLED, Array.Empty<OrderStatus>() }
        };

        if (validTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(newStatus))
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            throw new BusinessRuleViolationException(
                string.Format(DomainMessages.ORDER_STATUS_TRANSITION_INVALID, Status, newStatus));
        }
    }
}
