using SmartDine.Application.DTOs.Orders;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý nghiệp vụ đặt món — tạo đơn, cập nhật status, lấy danh sách.
/// </summary>
public class OrderService
{
    private readonly IUnitOfWork _uow;

    public OrderService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<OrderResponse> PlaceOrderAsync(Guid customerId, PlaceOrderRequest request)
    {
        // Validate menu items exist
        var menuItemIds = request.Items.Select(i => i.MenuItemId).ToList();
        var menuItems = await _uow.MenuItems.GetByIdsAsync(menuItemIds);

        if (menuItems.Count != menuItemIds.Count)
            throw new BusinessRuleViolationException("Một hoặc nhiều món không tồn tại trong menu.");

        var unavailable = menuItems.Where(m => !m.IsAvailable).Select(m => m.Name).ToList();
        if (unavailable.Any())
            throw new BusinessRuleViolationException($"Các món sau đang hết: {string.Join(", ", unavailable)}");

        // Create order
        var order = new Order
        {
            CustomerId = customerId,
            TableId = request.TableId,
            DiningSessionId = request.DiningSessionId,
            SpecialInstructions = request.SpecialInstructions,
            Status = OrderStatus.PENDING
        };

        // Create order items
        foreach (var itemRequest in request.Items)
        {
            var menuItem = menuItems.First(m => m.Id == itemRequest.MenuItemId);
            order.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                Quantity = itemRequest.Quantity,
                UnitPrice = menuItem.Price,
                SpecialInstructions = itemRequest.SpecialInstructions
            });
        }

        order.CalculateTotal();
        await _uow.Orders.AddAsync(order);
        await _uow.SaveChangesAsync();

        return MapToResponse(order, menuItems);
    }

    public async Task<OrderResponse> UpdateStatusAsync(Guid orderId, OrderStatus newStatus)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId)
            ?? throw new EntityNotFoundException("Order", orderId);

        order.UpdateStatus(newStatus); // Domain business rule validation
        await _uow.SaveChangesAsync();

        var menuItems = await _uow.MenuItems.GetByIdsAsync(
            order.Items.Select(i => i.MenuItemId).ToList());
        return MapToResponse(order, menuItems);
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid orderId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return null;

        var menuItems = await _uow.MenuItems.GetByIdsAsync(
            order.Items.Select(i => i.MenuItemId).ToList());
        return MapToResponse(order, menuItems);
    }

    public async Task<List<OrderResponse>> GetActiveOrdersAsync()
    {
        var orders = await _uow.Orders.GetActiveOrdersAsync();
        return orders.Select(o => MapToResponse(o,
            o.Items.Select(i => i.MenuItem).Where(m => m != null).ToList()!))
            .ToList();
    }

    public async Task<List<OrderResponse>> GetTodayOrdersAsync()
    {
        var orders = await _uow.Orders.GetTodayOrdersAsync();
        return orders.Select(o => MapToResponse(o,
            o.Items.Select(i => i.MenuItem).Where(m => m != null).ToList()!))
            .ToList();
    }

    public async Task<List<OrderResponse>> GetByCustomerIdAsync(Guid customerId, int page = 1, int pageSize = 20)
    {
        var orders = await _uow.Orders.GetByCustomerIdAsync(customerId, page, pageSize);
        return orders.Select(o => MapToResponse(o,
            o.Items.Select(i => i.MenuItem).Where(m => m != null).ToList()!))
            .ToList();
    }

    private static OrderResponse MapToResponse(Order order, IReadOnlyList<MenuItem> menuItems)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.User?.FullName,
            TableNumber = order.Table?.TableNumber ?? 0,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            SpecialInstructions = order.SpecialInstructions,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i =>
            {
                var menu = menuItems.FirstOrDefault(m => m.Id == i.MenuItemId);
                return new OrderItemResponse
                {
                    Id = i.Id,
                    MenuItemId = i.MenuItemId,
                    Name = menu?.Name ?? "Unknown",
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    SpecialInstructions = i.SpecialInstructions
                };
            }).ToList()
        };
    }
}
