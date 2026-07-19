using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.Settings;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;

namespace SmartDine.Identity.API.Controllers;

/// <summary>
/// Controller quản lý cấu hình chung của nhà hàng (restaurant_settings).
/// GET: diner/staff/manager đọc (để hóa đơn dùng đúng VAT + phí DV).
/// PATCH: chỉ MANAGER chỉnh.
/// </summary>
[ApiController]
[Route("api/v1/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly SettingsService _settingsService;

    public SettingsController(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// GET /api/v1/settings — Lấy cấu hình nhà hàng hiện tại (gồm TaxRate, ServiceChargeRate).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = Roles.AllExceptChef)]
    public async Task<IActionResult> Get()
    {
        var result = await _settingsService.GetAsync();
        return Ok(ApiResponse<SettingsResponse>.Ok(result));
    }

    /// <summary>
    /// PATCH /api/v1/settings — Cập nhật cấu hình nhà hàng (partial update).
    /// </summary>
    [HttpPatch]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request)
    {
        var result = await _settingsService.UpdateAsync(request);
        return Ok(ApiResponse<SettingsResponse>.Ok(result, ValidationMessages.SETTINGS_UPDATED_SUCCESS));
    }
}
