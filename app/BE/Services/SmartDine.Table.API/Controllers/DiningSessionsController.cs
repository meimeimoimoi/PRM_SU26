using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.DiningSessions;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;
using SmartDine.Domain.Enums;

namespace SmartDine.Table.API.Controllers;

/// <summary>
/// Controller quản lý phiên ăn uống đang diễn ra.
///
/// Endpoints:
///   API 1 — GET  /api/v1/dining-sessions/{id}/participants  → Danh sách thành viên cùng bàn.
///   API 2 — POST /api/v1/dining-sessions/{id}/leave         → Thực khách rời nhóm.
///   API 3 — GET  /api/v1/dining-sessions/{id}/bill-summary  → Tổng chi tiêu tạm tính.
///   API 4 — GET  /api/v1/dining-sessions/{id}/orders        → Danh sách món đã gọi.
/// </summary>
[ApiController]
[Route("api/v1/dining-sessions")]
[Authorize]
public class DiningSessionsController : ControllerBase
{
    private readonly DiningSessionService _sessionService;

    public DiningSessionsController(DiningSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    // ═══════════════════════════════════════════════════════════════
    // API 1: GET /api/v1/dining-sessions/{id}/participants
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Xem danh sách thành viên đang tham gia cùng bàn trong phiên ăn hiện tại.
    /// Dùng để hiển thị giao diện nhóm gọi món (ai là HOST, ai là MEMBER).
    /// </summary>
    [HttpGet("{id:int}/participants")]
    [Authorize(Roles = Roles.AllExceptChef)]
    public async Task<IActionResult> GetParticipants(int id)
    {
        var (customerId, guestSessionId) = ExtractIdentity();
        var result = await _sessionService.GetParticipantsAsync(id, customerId, guestSessionId, IsStaff());
        return Ok(ApiResponse<SessionParticipantsResponse>.Ok(result));
    }

    // ═══════════════════════════════════════════════════════════════
    // API 2: POST /api/v1/dining-sessions/{id}/leave
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Thực khách chủ động rời nhóm gọi món.
    /// Identity lấy từ JWT (không cần body).
    /// Nếu là HOST → chuyển quyền cho MEMBER tiếp theo.
    /// </summary>
    [HttpPost("{id:int}/leave")]
    [Authorize(Roles = Roles.AllDiners)]
    public async Task<IActionResult> Leave(int id)
    {
        var (customerId, guestSessionId) = ExtractIdentity();
        var result = await _sessionService.LeaveSessionAsync(id, customerId, guestSessionId);
        return Ok(ApiResponse<LeaveSessionResponse>.Ok(result, result.Message));
    }

    // ═══════════════════════════════════════════════════════════════
    // API 3: GET /api/v1/dining-sessions/{id}/bill-summary
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Xem tổng chi tiêu tạm tính của phiên ăn (chưa áp coupon).
    /// sub_total = tổng FinalAmount các order không CANCELLED.
    /// tax / service theo RestaurantSettings (Manager chỉnh được).
    /// </summary>
    [HttpGet("{id:int}/bill-summary")]
    [Authorize(Roles = Roles.AllExceptChef)]
    public async Task<IActionResult> GetBillSummary(int id)
    {
        var (customerId, guestSessionId) = ExtractIdentity();
        var result = await _sessionService.GetBillSummaryAsync(id, customerId, guestSessionId, IsStaff());
        return Ok(ApiResponse<BillSummaryResponse>.Ok(result));
    }

    // ═══════════════════════════════════════════════════════════════
    // API 4: GET /api/v1/dining-sessions/{id}/orders
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Lấy toàn bộ món đã gọi trong phiên ăn kèm trạng thái chế biến từng món.
    /// Dùng để khách theo dõi tiến độ hoặc nhân viên kiểm tra.
    /// </summary>
    [HttpGet("{id:int}/orders")]
    [Authorize(Roles = Roles.AllExceptChef)]
    public async Task<IActionResult> GetOrders(int id)
    {
        var (customerId, guestSessionId) = ExtractIdentity();
        var result = await _sessionService.GetSessionOrdersAsync(id, customerId, guestSessionId, IsStaff());
        return Ok(ApiResponse<SessionOrdersResponse>.Ok(result));
    }

    // ═══════════════════════════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Trích xuất định danh người dùng từ JWT.
    /// CUSTOMER → customerId (int).
    /// GUEST → guestSessionId (string sub claim).
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

    /// <summary>STAFF/MANAGER được xem mọi session, không bị giới hạn theo participant.</summary>
    private bool IsStaff() =>
        User.IsInRole(nameof(UserRole.STAFF)) ||
        User.IsInRole(nameof(UserRole.MANAGER));
}
