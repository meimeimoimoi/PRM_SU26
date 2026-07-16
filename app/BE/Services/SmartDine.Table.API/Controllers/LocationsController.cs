using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.Tables;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;

namespace SmartDine.Table.API.Controllers;

/// <summary>
/// Controller quản lý khu vực/vị trí bàn (VD: Tầng 1, Sân vườn, Phòng VIP) — dùng cho manager
/// dashboard khi tạo/sửa bàn ăn. Chỉ MANAGER truy cập (đây là master-data quản trị nội bộ,
/// không hiển thị cho khách).
/// </summary>
[ApiController]
[Route("api/v1/locations")]
[Authorize(Roles = Roles.Manager)]
public class LocationsController : ControllerBase
{
    private readonly TableService _tableService;

    public LocationsController(TableService tableService)
    {
        _tableService = tableService;
    }

    /// <summary>
    /// GET /api/v1/locations — Danh sách khu vực, dùng cho dropdown khi tạo/sửa bàn.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _tableService.GetAllLocationsAsync();
        return Ok(ApiResponse<List<LocationResponse>>.Ok(result));
    }

    /// <summary>
    /// POST /api/v1/locations — Tạo khu vực mới (khi dropdown chưa có lựa chọn phù hợp).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLocationRequest request)
    {
        var result = await _tableService.CreateLocationAsync(request);
        return Created("", ApiResponse<LocationResponse>.Ok(result, ValidationMessages.LOCATION_CREATED_SUCCESS));
    }
}
