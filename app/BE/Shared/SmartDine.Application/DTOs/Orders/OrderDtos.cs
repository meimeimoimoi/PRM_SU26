using System;
using System.Collections.Generic;
using SmartDine.Domain.Enums;

namespace SmartDine.Application.DTOs.Orders;

public class PlaceOrderRequest
{
    public int TableId { get; set; }
    public int DiningSessionId { get; set; }
    public string? SpecialInstructions { get; set; }
    public string? CouponCode { get; set; }
    public List<OrderDetailRequest> Items { get; set; } = new();
}

public class OrderDetailRequest
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = nameof(OrderStatus.PENDING);
}

public class UpdateItemsStatusRequest
{
    public List<int> ItemIds { get; set; } = new();
    public string Status { get; set; } = nameof(OrderDetailStatus.WAITING);
}

public class OrderResponse
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int TableId { get; set; }
    public int TableNumber { get; set; }
    public List<OrderDetailResponse> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = nameof(OrderStatus.PENDING);
    /// <summary>ACTIVE | CHECKOUT | CLOSED — CHECKOUT = đang chờ thanh toán (vd. tiền mặt tại quầy).</summary>
    public string SessionStatus { get; set; } = nameof(DiningSessionStatus.ACTIVE);
    /// <summary>Snapshot thuế/phí của phiên — null nếu phiên legacy chưa snapshot.</summary>
    public decimal? TaxRate { get; set; }
    public decimal? ServiceChargeRate { get; set; }
    public string? SpecialInstructions { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderDetailResponse
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Total => UnitPrice * Quantity;
    public string? Notes { get; set; }
    public string Status { get; set; } = nameof(OrderDetailStatus.WAITING);
}

public class OrderStatusResponse
{
    public int OrderId { get; set; }
    public string Status { get; set; } = nameof(OrderStatus.PENDING);
    public List<OrderItemStatusResponse> Items { get; set; } = new();
}

public class OrderItemStatusResponse
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = nameof(OrderDetailStatus.WAITING);
}
