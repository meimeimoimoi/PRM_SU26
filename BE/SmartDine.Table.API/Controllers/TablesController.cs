using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Tables;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;
using SmartDine.Domain.Enums;

namespace SmartDine.Table.API.Controllers;

/// <summary>
/// Controller quản lý bàn ăn và đặt bàn trước.
///
/// Tất cả endpoint yêu cầu JWT Bearer token.
/// Luồng request: Client → Gateway(:5000) → Table.API(:5004) → Controller → TableService → UnitOfWork → DB.
/// Exception được bắt bởi ExceptionHandlingMiddleware → trả về ApiResponse chuẩn với HTTP status tương ứng.
/// </summary>
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

    /// <summary>
    /// API 1 — Lấy danh sách bàn ăn, hỗ trợ filter.
    ///
    /// GET /api/v1/tables?status=AVAILABLE&amp;capacity=4
    /// Role: STAFF, MANAGER.
    ///
    /// Luồng: Nhận query params → Service validate status → Repo dynamic query → trả danh sách bàn.
    /// Nếu không truyền filter → trả tất cả bàn.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Roles.StaffAndManager)]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? capacity)
    {
        var result = await _tableService.GetAllAsync(status, capacity);
        return Ok(ApiResponse<List<TableResponse>>.Ok(result));
    }

    /// <summary>
    /// API 2 — Khách quét mã QR tại bàn.
    ///
    /// POST /api/v1/tables/{id}/scan
    /// Role: CUSTOMER, GUEST.
    ///
    /// Luồng: Khách scan QR trên bàn → app gửi tableId + customerId (nullable).
    ///   - Bàn trống → tạo DiningSession mới + chuyển bàn OCCUPIED.
    ///   - Bàn đã có khách → trả session hiện tại (tham gia nhóm gọi món).
    /// </summary>
    [HttpPost("{id:int}/scan")]
    [Authorize(Roles = Roles.AllDiners)]
    public async Task<IActionResult> ScanTable(int id, [FromBody] ScanTableRequest request)
    {
        // GUEST: guestSessionId = JWT sub claim (không phải customerId)
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == nameof(UserRole.GUEST))
            request.GuestSessionId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var result = await _tableService.ScanTableAsync(id, request);
        return Ok(ApiResponse<ScanTableResponse>.Ok(result, result.Message));
    }

    /// <summary>
    /// API 3 — Cập nhật trạng thái bàn ăn.
    ///
    /// PATCH /api/v1/tables/{id}/status
    /// Role: STAFF, MANAGER.
    ///
    /// Luồng: Nhân viên dọn bàn xong → bấm AVAILABLE → Service đóng DiningSession (nếu có).
    /// Hoặc chuyển bàn sang MAINTENANCE khi cần sửa chữa.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = Roles.StaffAndManager)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateTableStatusRequest request)
    {
        var result = await _tableService.UpdateStatusAsync(id, request.Status);
        return Ok(ApiResponse<UpdateTableStatusResponse>.Ok(result, ValidationMessages.TABLE_STATUS_UPDATED_SUCCESS));
    }

    /// <summary>
    /// API 4 — Đặt bàn trước (Booking).
    ///
    /// POST /api/v1/tables/reservations
    /// Role: CUSTOMER, STAFF, MANAGER.
    ///
    /// Luồng: Khách/nhân viên gửi thông tin đặt bàn → Service validate (sức chứa, thời gian, xung đột)
    ///        → tạo reservation PENDING → trả về thông tin đặt bàn.
    /// </summary>
    [HttpPost("reservations")]
    [Authorize(Roles = Roles.CustomerAndManagement)]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
    {
        var result = await _tableService.CreateReservationAsync(request);
        return Created("", ApiResponse<ReservationResponse>.Ok(result, ValidationMessages.RESERVATION_CREATED_SUCCESS));
    }

    /// <summary>
    /// API 5 — Cập nhật trạng thái đặt bàn.
    ///
    /// PATCH /api/v1/tables/reservations/{id}/status
    /// Role: STAFF, MANAGER.
    ///
    /// Luồng: Khách đến → nhân viên bấm CHECKED_IN → Service chuyển bàn OCCUPIED + tạo DiningSession.
    ///        Hoặc CANCELLED (hủy), NO_SHOW (khách không đến).
    /// </summary>
    [HttpPatch("reservations/{id:int}/status")]
    [Authorize(Roles = Roles.StaffAndManager)]
    public async Task<IActionResult> UpdateReservationStatus(int id, [FromBody] UpdateReservationStatusRequest request)
    {
        var result = await _tableService.UpdateReservationStatusAsync(id, request.Status);
        return Ok(ApiResponse<UpdateReservationStatusResponse>.Ok(result, ValidationMessages.RESERVATION_STATUS_UPDATED_SUCCESS));
    }
}
