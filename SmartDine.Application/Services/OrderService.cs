using SmartDine.Application.DTOs;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý nghiệp vụ đặt món cho nhà hàng.
/// Tính toán tổng tiền, tạo đơn hàng, gọi Repository để lưu.
/// </summary>
public class OrderService
{
    private readonly IOrderRepository _orderRepository;

    // Dữ liệu menu giả lập (sau này sẽ lấy từ DB)
    private readonly List<MenuItem> _menu = new()
    {
        new MenuItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Phở Bò", Price = 55000 },
        new MenuItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Bún Chả", Price = 45000 },
        new MenuItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Cơm Tấm", Price = 50000 },
        new MenuItem { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Bánh Mì", Price = 25000 },
        new MenuItem { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Trà Đá", Price = 5000 },
    };

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Đặt món: Tìm món trong menu → tính tổng tiền → tạo Order → lưu vào Repository.
    /// </summary>
    public OrderResponse PlaceOrder(PlaceOrderRequest request)
    {
        // Tìm các món ăn theo Id
        var orderedItems = _menu
            .Where(m => request.MenuItemIds.Contains(m.Id))
            .ToList();

        if (orderedItems.Count == 0)
            throw new ArgumentException("Không tìm thấy món ăn nào trong menu!");

        // Tính tổng tiền
        var totalAmount = orderedItems.Sum(item => item.Price);

        // Tạo đơn hàng mới
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Items = orderedItems,
            TotalAmount = totalAmount,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        // Lưu vào Repository
        _orderRepository.Add(order);

        // Map sang Response DTO để trả về cho Frontend
        return MapToResponse(order);
    }

    /// <summary>
    /// Lấy đơn hàng theo Id.
    /// </summary>
    public OrderResponse? GetOrderById(Guid id)
    {
        var order = _orderRepository.GetById(id);
        return order is null ? null : MapToResponse(order);
    }

    /// <summary>
    /// Lấy tất cả đơn hàng.
    /// </summary>
    public IEnumerable<OrderResponse> GetAllOrders()
    {
        return _orderRepository.GetAll().Select(MapToResponse);
    }

    /// <summary>
    /// Lấy danh sách thực đơn.
    /// </summary>
    public IEnumerable<OrderItemResponse> GetMenu()
    {
        return _menu.Select(m => new OrderItemResponse
        {
            Id = m.Id,
            Name = m.Name,
            Price = m.Price
        });
    }

    /// <summary>
    /// Map từ Entity sang DTO.
    /// </summary>
    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            TotalAmount = order.TotalAmount,
            Status = Enum.Parse<OrderStatus>(order.Status),
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemResponse
            {
                Id = i.Id,
                Name = i.Name,
                Price = i.Price
            }).ToList()
        };
    }
}
