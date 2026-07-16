using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Orders;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;
using SmartDine.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDine.Order.API.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>POST /api/v1/orders — Đặt món mới (DINER/GUEST/STAFF)</summary>
    [HttpPost]
    [Authorize(Roles = Roles.AllDinersAndStaff)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var (customerId, guestSessionId) = ExtractIdentity();
        var result = await _orderService.PlaceOrderAsync(customerId, guestSessionId, IsStaff(), request);
        return Created("", ApiResponse<OrderResponse>.Ok(result, ValidationMessages.ORDER_PLACED_SUCCESS));
    }

    /// <summary>GET /api/v1/orders/{id} — Lấy chi tiết đơn hàng</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _orderService.GetByIdAsync(id);
        return Ok(ApiResponse<OrderResponse>.Ok(result));
    }

    /// <summary>GET /api/v1/orders/{id}/status — Theo dõi tiến độ món ăn realtime</summary>
    [HttpGet("{id:int}/status")]
    public async Task<IActionResult> GetStatus(int id)
    {
        var (customerId, guestSessionId) = ExtractIdentity();
        var result = await _orderService.GetStatusAsync(id, customerId, guestSessionId, IsStaff());
        return Ok(ApiResponse<OrderStatusResponse>.Ok(result));
    }

    /// <summary>GET /api/v1/orders/active — Đơn hàng đang hoạt động (cho kitchen/staff)</summary>
    [HttpGet("active")]
    [Authorize(Roles = Roles.KitchenStaff)]
    public async Task<IActionResult> GetActiveOrders()
    {
        var result = await _orderService.GetActiveOrdersAsync();
        return Ok(ApiResponse<List<OrderResponse>>.Ok(result));
    }

    /// <summary>GET /api/v1/orders/today — Tất cả đơn hôm nay (cho staff/manager)</summary>
    [HttpGet("today")]
    [Authorize(Roles = Roles.KitchenStaff)]
    public async Task<IActionResult> GetTodayOrders()
    {
        var result = await _orderService.GetTodayOrdersAsync();
        return Ok(ApiResponse<List<OrderResponse>>.Ok(result));
    }

    /// <summary>
    /// GET /api/v1/orders/chart?period=day|week|month — Doanh số theo đơn hàng cho Dashboard Manager.
    /// Không lọc theo thanh toán (khác /payments/chart) — dùng để xem mức độ hoạt động/đơn đặt.
    /// </summary>
    [HttpGet("chart")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> GetOrderChart([FromQuery] string period = "day")
    {
        var result = await _orderService.GetOrderChartAsync(period);
        return Ok(ApiResponse<List<ChartPointResponse>>.Ok(result));
    }

    /// <summary>PATCH /api/v1/orders/{id}/status — Cập nhật trạng thái đơn hàng</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = Roles.KitchenStaff)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateStatusAsync(id, request.Status);
        return Ok(ApiResponse<OrderResponse>.Ok(result, ValidationMessages.ORDER_STATUS_UPDATED_SUCCESS));
    }

    /// <summary>PATCH /api/v1/orders/items/{itemId}/status — Cập nhật trạng thái món ăn trong đơn hàng</summary>
    [HttpPatch("items/{itemId:int}/status")]
    [Authorize(Roles = Roles.KitchenStaff)]
    public async Task<IActionResult> UpdateItemStatus(int itemId, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateItemStatusAsync(itemId, request.Status);
        return Ok(ApiResponse<bool>.Ok(result, ValidationMessages.ORDER_STATUS_UPDATED_SUCCESS));
    }

    /// <summary>GET /api/v1/orders/my — Lịch sử đơn hàng của customer</summary>
    [HttpGet("my")]
    [Authorize(Roles = Roles.Customer)]
    public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _orderService.GetByCustomerIdAsync(customerId, page, pageSize);
        return Ok(ApiResponse<List<OrderResponse>>.Ok(result));
    }

    /// <summary>GET /api/v1/orders/session — Lịch sử đơn hàng của CUSTOMER hoặc GUEST (dựa trên JWT)</summary>
    [HttpGet("session")]
    [Authorize(Roles = Roles.AllDinersAndStaff)]
    public async Task<IActionResult> GetSessionOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (customerId, guestSessionId) = ExtractIdentity();

        if (customerId.HasValue)
        {
            var result = await _orderService.GetByCustomerIdAsync(customerId.Value, page, pageSize);
            return Ok(ApiResponse<List<OrderResponse>>.Ok(result));
        }

        if (!string.IsNullOrEmpty(guestSessionId))
        {
            var result = await _orderService.GetByGuestSessionIdAsync(guestSessionId, page, pageSize);
            return Ok(ApiResponse<List<OrderResponse>>.Ok(result));
        }

        return Ok(ApiResponse<List<OrderResponse>>.Ok(new List<OrderResponse>()));
    }

    /// <summary>GET /api/v1/orders/session/{sessionId} — Toàn bộ đơn hàng trong phiên ăn (mọi participant cùng bàn)</summary>
    [HttpGet("session/{sessionId:int}")]
    [Authorize(Roles = Roles.AllDinersAndStaff)]
    public async Task<IActionResult> GetOrdersBySession(int sessionId)
    {
        var (customerId, guestSessionId) = ExtractIdentity();
        var result = await _orderService.GetBySessionIdAsync(sessionId, customerId, guestSessionId, IsStaff());
        return Ok(ApiResponse<List<OrderResponse>>.Ok(result));
    }

    // ═══════════════════════════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Trích xuất định danh người dùng từ JWT.
    /// CUSTOMER → customerId (int). GUEST → guestSessionId (string sub claim).
    /// </summary>
    private (int? customerId, string? guestSessionId) ExtractIdentity()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (role == nameof(UserRole.CUSTOMER) && int.TryParse(sub, out var cid))
            return (cid, null);

        if (role == nameof(UserRole.GUEST))
            return (null, sub);

        return (null, null);
    }

    /// <summary>STAFF được đặt/xem mọi session, không bị giới hạn theo participant.</summary>
    private bool IsStaff() =>
        User.IsInRole(nameof(UserRole.STAFF)) ||
        User.IsInRole(nameof(UserRole.CHEF)) ||
        User.IsInRole(nameof(UserRole.MANAGER));
}
