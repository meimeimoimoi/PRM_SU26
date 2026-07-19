using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.Orders;
using SmartDine.Application.Helper;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý nghiệp vụ đặt món — tạo đơn, áp coupon, cập nhật status, theo dõi tiến độ.
/// </summary>
public class OrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IOrderNotificationService _notificationService;

    public OrderService(IUnitOfWork uow, IOrderNotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

    // ═══════════════════════════════════════════════════════════════
    // POST /api/v1/orders
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Đặt món: chuyển giỏ hàng hiện tại của phiên ăn thành Order chính thức.
    ///
    /// Identity: CUSTOMER → callerCustomerId; GUEST → callerGuestSessionId; STAFF → cả hai null + isStaff.
    /// Ownership: CUSTOMER/GUEST chỉ đặt được cho session mình đang là participant active. STAFF không giới hạn.
    /// Coupon: chỉ áp dụng khi caller là CUSTOMER (có callerCustomerId) — GUEST/STAFF gửi coupon_code sẽ bị bỏ qua.
    /// </summary>
    public async Task<OrderResponse> PlaceOrderAsync(
        int? callerCustomerId, string? callerGuestSessionId, bool isStaff, PlaceOrderRequest request)
    {
        var session = await _uow.DiningSessions.GetByIdWithParticipantsAsync(request.DiningSessionId)
            ?? throw new EntityNotFoundException("Dining Session", request.DiningSessionId);

        if (session.Status == DiningSessionStatus.CHECKOUT)
            throw new BusinessRuleViolationException(ValidationMessages.ORDER_BLOCKED_CHECKOUT);
        if (session.Status != DiningSessionStatus.ACTIVE)
            throw new BusinessRuleViolationException(ValidationMessages.DINING_SESSION_NOT_ACTIVE);

        EnsureCallerIsParticipant(session.Participants, callerCustomerId, callerGuestSessionId, isStaff);

        // Validate menu items exist
        var menuItemIds = request.Items.Select(i => i.MenuItemId).ToList();
        var menuItems = await _uow.MenuItems.GetByIdsAsync(menuItemIds);

        if (menuItems.Count != menuItemIds.Count)
            throw new BusinessRuleViolationException(ValidationMessages.ORDER_ITEM_NOT_IN_MENU);

        var unavailable = menuItems.Where(m => !m.IsAvailable).Select(m => m.Name).ToList();
        if (unavailable.Any())
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.ORDER_ITEM_UNAVAILABLE, string.Join(", ", unavailable)));

        // Create order
        var order = new Order
        {
            SessionId = session.Id,
            Status = OrderStatus.PENDING
        };

        // Create order items
        foreach (var itemRequest in request.Items)
        {
            var menuItem = menuItems.First(m => m.Id == itemRequest.MenuItemId);
            order.OrderDetails.Add(new OrderDetail
            {
                MenuItemId = menuItem.Id,
                Quantity = itemRequest.Quantity,
                UnitPrice = menuItem.Price,
                Notes = itemRequest.Notes,
                Status = OrderDetailStatus.WAITING
            });
        }

        order.CalculateTotal();

        // Áp coupon — chỉ cho CUSTOMER tự đặt qua tài khoản mình (GUEST/STAFF: bỏ qua âm thầm)
        if (!string.IsNullOrWhiteSpace(request.CouponCode) && callerCustomerId.HasValue)
            await ApplyCouponAsync(order, callerCustomerId.Value, request.CouponCode);

        await _uow.Orders.AddAsync(order);
        await _uow.SaveChangesAsync();

        // Gửi thông báo thời gian thực đến nhà bếp
        await _notificationService.NotifyNewOrderAsync(order.Id, session.Table.TableNumber, order.FinalAmount);

        // session đã include Table + Customer + Participants → map response không cần query thêm
        order.Session = session;

        return MapToResponse(order, menuItems);
    }

    private async Task ApplyCouponAsync(Order order, int customerId, string couponCode)
    {
        var promotion = await _uow.Coupons.GetActivePromotionByCodeAsync(couponCode)
            ?? throw new BusinessRuleViolationException(ValidationMessages.COUPON_NOT_FOUND);

        var now = DateTime.UtcNow;
        if (now < promotion.StartDate || now > promotion.EndDate)
            throw new BusinessRuleViolationException(ValidationMessages.COUPON_EXPIRED);

        if (promotion.DiscountType != PromotionType.PERCENT && promotion.DiscountType != PromotionType.FIXED)
            throw new BusinessRuleViolationException(ValidationMessages.COUPON_NOT_SUPPORTED_TYPE);

        var customerCoupon = await _uow.Coupons.GetByCustomerAndPromotionAsync(customerId, promotion.Id)
            ?? throw new BusinessRuleViolationException(ValidationMessages.COUPON_NOT_OWNED);

        if (customerCoupon.IsUsed)
            throw new BusinessRuleViolationException(ValidationMessages.COUPON_ALREADY_USED);

        order.DiscountAmount = promotion.DiscountType == PromotionType.PERCENT
            ? Math.Round(order.TotalAmount * promotion.DiscountValue / 100m, 2)
            : promotion.DiscountValue;
        order.CalculateTotal();

        customerCoupon.IsUsed = true;
        customerCoupon.UsedAt = now;
        await _uow.Coupons.UpdateAsync(customerCoupon);
    }

    public async Task<OrderResponse> UpdateStatusAsync(int orderId, string newStatus)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId)
            ?? throw new EntityNotFoundException("Order", orderId);

        if (!Enum.TryParse<OrderStatus>(newStatus, true, out var parsedStatus))
            throw new BusinessRuleViolationException(ValidationMessages.ORDER_STATUS_INVALID);

        order.UpdateStatus(parsedStatus);

        var detailStatus = parsedStatus switch
        {
            OrderStatus.COOKING   => OrderDetailStatus.DOING,
            OrderStatus.READY     => OrderDetailStatus.DONE,
            OrderStatus.COMPLETED => OrderDetailStatus.SERVED,
            OrderStatus.CANCELLED => OrderDetailStatus.CANCELLED,
            _                     => (OrderDetailStatus?)null
        };

        if (detailStatus.HasValue)
        {
            foreach (var detail in order.OrderDetails.Where(d => d.Status != OrderDetailStatus.CANCELLED))
                detail.Status = detailStatus.Value;
        }

        await _uow.SaveChangesAsync();

        // Gửi thông báo thời gian thực đến khách hàng tại bàn ăn
        await _notificationService.NotifyOrderStatusChangedAsync(order.Id, order.Session.TableId, newStatus);

        var menuItems = await _uow.MenuItems.GetByIdsAsync(
            order.OrderDetails.Select(i => i.MenuItemId).ToList());
        return MapToResponse(order, menuItems);
    }

    public async Task<bool> UpdateItemStatusAsync(int itemId, string newStatus)
    {
        var item = await _uow.OrderDetails.GetByIdAsync(itemId);
        if (item == null)
            throw new EntityNotFoundException("OrderDetail", itemId);

        if (!Enum.TryParse<OrderDetailStatus>(newStatus, true, out var parsedStatus))
            throw new BusinessRuleViolationException(ValidationMessages.ORDER_STATUS_INVALID);

        item.Status = parsedStatus;
        if (parsedStatus == OrderDetailStatus.DONE)
        {
            item.CompletedAt = DateTime.UtcNow;
        }

        var order = await _uow.Orders.GetByIdAsync(item.OrderId);
        if (order != null)
        {
            var activeItems = order.OrderDetails.Where(d => d.Status != OrderDetailStatus.CANCELLED && d.Status != OrderDetailStatus.RETURNED).ToList();
            if (activeItems.Any())
            {
                // Suy ra trạng thái đơn từ trạng thái các item, nhưng áp dụng qua cùng
                // transition graph với UpdateStatusAsync (order.CanTransitionTo) để 2 luồng
                // cập nhật (theo đơn / theo từng item) không bao giờ đưa Order vào trạng thái
                // trái quy tắc state machine. Nếu bước suy ra không phải transition hợp lệ từ
                // trạng thái hiện tại (vd item bị đánh dấu DONE trong khi đơn đang PENDING,
                // bỏ qua COOKING), giữ nguyên order.Status thay vì ép trạng thái không hợp lệ.
                OrderStatus? derivedStatus = activeItems.All(d => d.Status == OrderDetailStatus.SERVED)
                    ? OrderStatus.COMPLETED
                    : activeItems.All(d => d.Status == OrderDetailStatus.DONE || d.Status == OrderDetailStatus.SERVED)
                        ? OrderStatus.READY
                        : activeItems.Any(d => d.Status == OrderDetailStatus.DOING || d.Status == OrderDetailStatus.DONE || d.Status == OrderDetailStatus.SERVED)
                            ? OrderStatus.COOKING
                            : null;

                if (derivedStatus.HasValue && order.CanTransitionTo(derivedStatus.Value))
                {
                    order.UpdateStatus(derivedStatus.Value);
                }
            }
        }

        await _uow.SaveChangesAsync();

        if (order != null)
        {
            await _notificationService.NotifyOrderStatusChangedAsync(order.Id, order.Session.TableId, order.Status.ToString());
        }

        return true;
    }

    public async Task<OrderResponse> GetByIdAsync(int orderId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId)
            ?? throw new EntityNotFoundException("Order", orderId);

        var menuItems = await _uow.MenuItems.GetByIdsAsync(
            order.OrderDetails.Select(i => i.MenuItemId).ToList());
        return MapToResponse(order, menuItems);
    }

    // ═══════════════════════════════════════════════════════════════
    // GET /api/v1/orders/{id}/status
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Theo dõi tiến độ chế biến từng món trong đơn — dùng cho thanh trạng thái realtime phía Client.
    /// Ownership: CUSTOMER/GUEST chỉ xem được đơn thuộc session mình đang là participant. STAFF/CHEF/MANAGER không giới hạn.
    /// </summary>
    public async Task<OrderStatusResponse> GetStatusAsync(
        int orderId, int? callerCustomerId, string? callerGuestSessionId, bool isStaff)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId)
            ?? throw new EntityNotFoundException("Order", orderId);

        EnsureCallerIsParticipant(order.Session.Participants, callerCustomerId, callerGuestSessionId, isStaff);

        return new OrderStatusResponse
        {
            OrderId = order.Id,
            Status = order.Status.ToString(),
            Items = order.OrderDetails.Select(d => new OrderItemStatusResponse
            {
                Name = d.MenuItem?.Name ?? "Unknown",
                Quantity = d.Quantity,
                Status = d.Status.ToString()
            }).ToList()
        };
    }

    public async Task<List<OrderResponse>> GetActiveOrdersAsync()
    {
        var orders = await _uow.Orders.GetActiveOrdersAsync();
        return orders.Select(o => MapToResponse(o,
            o.OrderDetails.Select(i => i.MenuItem).Where(m => m != null).ToList()!))
            .ToList();
    }

    public async Task<List<OrderResponse>> GetTodayOrdersAsync()
    {
        var orders = await _uow.Orders.GetTodayOrdersAsync();
        return orders.Select(o => MapToResponse(o,
            o.OrderDetails.Select(i => i.MenuItem).Where(m => m != null).ToList()!))
            .ToList();
    }

    /// <summary>
    /// Chart doanh số theo đơn hàng (không lọc thanh toán) cho Dashboard Manager — "Order Sales".
    /// period: day (theo giờ hôm nay) | week (7 ngày) | month (từ đầu tháng).
    /// </summary>
    public async Task<List<ChartPointResponse>> GetOrderChartAsync(string? period)
    {
        var (start, end) = ChartPeriodHelper.ResolveRange(period);
        var orders = await _uow.Orders.GetByDateRangeAsync(start, end);
        return ChartPeriodHelper.Bucket(orders.Select(o => (o.CreatedAt, o.FinalAmount)), period);
    }

    public async Task<List<OrderResponse>> GetByCustomerIdAsync(int customerId, int page = 1, int pageSize = 20)
    {
        var customer = await _uow.Customers.GetByIdAsync(customerId);
        if (customer == null) return new List<OrderResponse>();

        var orders = await _uow.Orders.GetByCustomerIdAsync(customer.Id, page, pageSize);
        return orders.Select(o => MapToResponse(o,
            o.OrderDetails.Select(i => i.MenuItem).Where(m => m != null).ToList()!))
            .ToList();
    }

    public async Task<List<OrderResponse>> GetByGuestSessionIdAsync(string guestSessionId, int page = 1, int pageSize = 20)
    {
        var orders = await _uow.Orders.GetByGuestSessionIdAsync(guestSessionId, page, pageSize);
        return orders.Select(o => MapToResponse(o,
            o.OrderDetails.Select(i => i.MenuItem).Where(m => m != null).ToList()!))
            .ToList();
    }

    /// <summary>
    /// GET /api/v1/orders/session/{sessionId} — Toàn bộ đơn hàng trong phiên ăn (mọi participant),
    /// khác với GetByGuestSessionIdAsync vốn chỉ trả đơn của riêng lần đăng nhập GUEST hiện tại.
    /// </summary>
    public async Task<List<OrderResponse>> GetBySessionIdAsync(
        int sessionId, int? callerCustomerId, string? callerGuestSessionId, bool isStaff)
    {
        var session = await _uow.DiningSessions.GetByIdWithParticipantsAsync(sessionId)
            ?? throw new EntityNotFoundException("DiningSession", sessionId);

        EnsureCallerIsParticipant(session.Participants, callerCustomerId, callerGuestSessionId, isStaff);

        var orders = await _uow.Orders.GetByDiningSessionIdAsync(sessionId);
        return orders.Select(o => MapToResponse(o,
            o.OrderDetails.Select(i => i.MenuItem).Where(m => m != null).ToList()!))
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Chặn truy cập trái phép: STAFF xem/đặt được mọi session; CUSTOMER/GUEST chỉ thao tác được
    /// session mà chính họ đang là thành viên đang hoạt động (chưa rời). Cùng pattern với DiningSessionService.
    /// </summary>
    private static void EnsureCallerIsParticipant(
        IEnumerable<SessionParticipant> participants,
        int? callerCustomerId,
        string? callerGuestSessionId,
        bool isStaff)
    {
        if (isStaff)
            return;

        var isParticipant = participants.Any(p =>
            p.IsActive &&
            ((callerCustomerId.HasValue && p.CustomerId == callerCustomerId) ||
             (!string.IsNullOrEmpty(callerGuestSessionId) && p.GuestSessionId == callerGuestSessionId)));

        if (!isParticipant)
            throw new UnauthorizedAccessException(ValidationMessages.ORDER_SESSION_ACCESS_DENIED);
    }

    private static OrderResponse MapToResponse(Order order, IReadOnlyList<MenuItem> menuItems)
    {
        return new OrderResponse
        {
            Id = order.Id,
            SessionId = order.SessionId,
            CustomerId = order.Session?.CustomerId,
            CustomerName = order.Session?.Customer?.FullName ?? order.Session?.GuestName,
            TableId = order.Session?.TableId ?? 0,
            TableNumber = order.Session?.Table?.TableNumber ?? 0,
            TotalAmount = order.TotalAmount,
            DiscountAmount = order.DiscountAmount,
            FinalAmount = order.FinalAmount,
            Status = order.Status.ToString(),
            SessionStatus = order.Session?.Status.ToString() ?? nameof(DiningSessionStatus.ACTIVE),
            CreatedAt = order.CreatedAt,
            Items = order.OrderDetails.Select(i =>
            {
                var menu = menuItems.FirstOrDefault(m => m.Id == i.MenuItemId);
                return new OrderDetailResponse
                {
                    Id = i.Id,
                    MenuItemId = i.MenuItemId,
                    Name = menu?.Name ?? "Unknown",
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Notes = i.Notes,
                    Status = i.Status.ToString()
                };
            }).ToList()
        };
    }
}
