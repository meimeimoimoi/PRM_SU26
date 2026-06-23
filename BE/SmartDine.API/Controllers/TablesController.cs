using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.Tables;
using SmartDine.Application.Services;

namespace SmartDine.API.Controllers;

[ApiController]
[Route("api/v1/tables")]
[Authorize]
public class TablesController : ControllerBase
{
    private readonly TableService _tableService;

    public TablesController(TableService tableService)
    {
        _tableService = tableService;
    }

    /// <summary>GET /api/v1/tables — Danh sách bàn ăn (filter theo status, capacity)</summary>
    [HttpGet]
    [Authorize(Roles = "STAFF,MANAGER")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? capacity)
    {
        var result = await _tableService.GetAllAsync(status, capacity);
        return Ok(ApiResponse<List<TableResponse>>.Ok(result));
    }

    /// <summary>POST /api/v1/tables/{id}/scan — Khách quét QR tại bàn</summary>
    [HttpPost("{id:int}/scan")]
    [Authorize(Roles = "CUSTOMER,GUEST")]
    public async Task<IActionResult> ScanTable(int id, [FromBody] ScanTableRequest request)
    {
        var result = await _tableService.ScanTableAsync(id, request);
        return Ok(ApiResponse<ScanTableResponse>.Ok(result, result.Message));
    }

    /// <summary>PATCH /api/v1/tables/{id}/status — Cập nhật trạng thái bàn</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "STAFF,MANAGER")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTableStatusRequest request)
    {
        var result = await _tableService.UpdateStatusAsync(id, request.Status);
        return Ok(ApiResponse<UpdateTableStatusResponse>.Ok(result, "Cập nhật trạng thái bàn thành công"));
    }

    /// <summary>POST /api/v1/tables/reservations — Đặt bàn trước</summary>
    [HttpPost("reservations")]
    [Authorize(Roles = "CUSTOMER,STAFF,MANAGER")]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
    {
        var result = await _tableService.CreateReservationAsync(request);
        return Created("", ApiResponse<ReservationResponse>.Ok(result, "Đặt bàn thành công"));
    }

    /// <summary>PATCH /api/v1/tables/reservations/{id}/status — Cập nhật trạng thái đặt bàn</summary>
    [HttpPatch("reservations/{id:int}/status")]
    [Authorize(Roles = "STAFF,MANAGER")]
    public async Task<IActionResult> UpdateReservationStatus(int id, [FromBody] UpdateReservationStatusRequest request)
    {
        var result = await _tableService.UpdateReservationStatusAsync(id, request.Status);
        return Ok(ApiResponse<UpdateReservationStatusResponse>.Ok(result, "Cập nhật trạng thái đặt bàn thành công"));
    }
}
