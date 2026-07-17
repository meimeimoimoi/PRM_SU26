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

    private static readonly Dictionary<OrderStatus, OrderStatus[]> ValidTransitions = new()
    {
        { OrderStatus.PENDING,   new[] { OrderStatus.CONFIRMED, OrderStatus.COOKING, OrderStatus.CANCELLED } },
        { OrderStatus.CONFIRMED, new[] { OrderStatus.COOKING, OrderStatus.CANCELLED } },
        { OrderStatus.COOKING,   new[] { OrderStatus.READY, OrderStatus.CANCELLED } },
        { OrderStatus.READY,     new[] { OrderStatus.COMPLETED } },
        { OrderStatus.COMPLETED, Array.Empty<OrderStatus>() },
        { OrderStatus.CANCELLED, Array.Empty<OrderStatus>() }
    };

    /// <summary>
    /// Đây có phải là bước chuyển trạng thái hợp lệ từ trạng thái hiện tại không.
    /// Dùng để đồng bộ hóa order status suy ra từ item status (UpdateItemStatusAsync)
    /// với cùng một quy tắc chuyển trạng thái mà UpdateStatus áp dụng, tránh 2 luồng
    /// cập nhật đưa Order vào trạng thái không hợp lệ theo state machine.
    /// </summary>
    public bool CanTransitionTo(OrderStatus newStatus)
        => ValidTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(newStatus);

    /// <summary>
    /// Cập nhật trạng thái với business validation.
    /// </summary>
    public void UpdateStatus(OrderStatus newStatus)
    {
        if (CanTransitionTo(newStatus))
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
