using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.Staff;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;

namespace SmartDine.Identity.API.Controllers;

/// <summary>
/// Controller quản lý tài khoản nhân viên nội bộ (STAFF/CHEF/MANAGER) — dùng cho manager dashboard.
///
/// Toàn bộ endpoint yêu cầu role MANAGER.
/// Luồng request: Client → Gateway(:5000) → Identity.API(:5001) → StaffController → StaffService.
/// </summary>
[ApiController]
[Route("api/v1/staff")]
[Authorize(Roles = Roles.Manager)]
public class StaffController : ControllerBase
{
    private readonly StaffService _staffService;

    public StaffController(StaffService staffService)
    {
        _staffService = staffService;
    }

    /// <summary>
    /// GET /api/v1/staff?role=&amp;isActive=&amp;page=&amp;pageSize= — Danh sách nhân viên phân trang.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, total, totalPages) = await _staffService.GetAllAsync(role, isActive, page, pageSize);
        return Ok(PaginatedApiResponse<StaffResponse>.Ok(items, total, page, totalPages));
    }

    /// <summary>
    /// GET /api/v1/staff/{id} — Chi tiết 1 nhân viên.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _staffService.GetByIdAsync(id);
        return Ok(ApiResponse<StaffResponse>.Ok(result));
    }

    /// <summary>
    /// POST /api/v1/staff — Tạo tài khoản nhân viên mới.
    /// Manager đặt mật khẩu trực tiếp cho nhân viên.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request)
    {
        var result = await _staffService.CreateAsync(request);
        return Created("", ApiResponse<StaffResponse>.Ok(result, ValidationMessages.STAFF_CREATED_SUCCESS));
    }

    /// <summary>
    /// PATCH /api/v1/staff/{id} — Cập nhật thông tin/role nhân viên (partial update).
    /// </summary>
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStaffRequest request)
    {
        var result = await _staffService.UpdateAsync(id, request);
        return Ok(ApiResponse<StaffResponse>.Ok(result, ValidationMessages.STAFF_UPDATED_SUCCESS));
    }

    /// <summary>
    /// DELETE /api/v1/staff/{id} — Vô hiệu hóa tài khoản nhân viên (IsActive = false).
    /// Manager không thể tự vô hiệu hóa chính mình.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var callerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _staffService.DeactivateAsync(id, callerId);
        return Ok(ApiResponse<object>.Ok(null!, ValidationMessages.STAFF_DEACTIVATED_SUCCESS));
    }
}
