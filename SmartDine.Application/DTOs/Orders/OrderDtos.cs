using SmartDine.Domain.Enums;

namespace SmartDine.Application.DTOs.Orders;

public class PlaceOrderRequest
{
    public Guid TableId { get; set; }
    public Guid? DiningSessionId { get; set; }
    public string? PromotionCode { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? SpecialInstructions { get; set; }
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}

public class OrderResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int TableNumber { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string? SpecialInstructions { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderItemResponse
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Total => UnitPrice * Quantity;
    public string? SpecialInstructions { get; set; }
}
