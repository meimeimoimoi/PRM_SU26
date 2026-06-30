using System;
using System.Collections.Generic;

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
    public string Status { get; set; } = "PENDING";
}

public class OrderResponse
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int TableNumber { get; set; }
    public List<OrderDetailResponse> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = "PENDING";
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
    public string Status { get; set; } = "WAITING";
}

public class OrderStatusResponse
{
    public int OrderId { get; set; }
    public string Status { get; set; } = "PENDING";
    public List<OrderItemStatusResponse> Items { get; set; } = new();
}

public class OrderItemStatusResponse
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = "WAITING";
}
