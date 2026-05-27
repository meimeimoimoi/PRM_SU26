using SmartDine.Domain.Enums;

namespace SmartDine.Application.DTOs;

/// <summary>
/// DTO trả dữ liệu đơn hàng về cho Frontend.
/// </summary>
public class OrderResponse
{
    public Guid Id { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Thông tin chi tiết từng món trong đơn hàng.
/// </summary>
public class OrderItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
