using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.DTOs;
using SmartDine.Application.Services;

namespace SmartDine.API.Controllers;

/// <summary>
/// API Controller xử lý các yêu cầu liên quan đến đơn hàng.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// POST /api/orders — Đặt món mới.
    /// </summary>
    [HttpPost]
    public IActionResult PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        try
        {
            var response = _orderService.PlaceOrder(request);
            return CreatedAtAction(nameof(GetOrderById), new { id = response.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/orders/{id} — Lấy đơn hàng theo Id.
    /// </summary>
    [HttpGet("{id:guid}")]
    public IActionResult GetOrderById(Guid id)
    {
        var order = _orderService.GetOrderById(id);
        if (order is null)
            return NotFound(new { error = $"Không tìm thấy đơn hàng với Id: {id}" });

        return Ok(order);
    }

    /// <summary>
    /// GET /api/orders — Lấy tất cả đơn hàng.
    /// </summary>
    [HttpGet]
    public IActionResult GetAllOrders()
    {
        var orders = _orderService.GetAllOrders();
        return Ok(orders);
    }

    /// <summary>
    /// GET /api/orders/menu — Lấy danh sách thực đơn.
    /// </summary>
    [HttpGet("menu")]
    public IActionResult GetMenu()
    {
        var menu = _orderService.GetMenu();
        return Ok(menu);
    }
}
