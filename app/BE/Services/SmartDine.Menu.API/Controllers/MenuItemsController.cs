using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.Menu;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;
using SmartDine.Domain.Enums;

namespace SmartDine.Menu.API.Controllers;

/// <summary>
/// Controller quản lý thực đơn món ăn (menu_items).
///
/// Endpoints:
///   API 1 — GET    /api/v1/menu-items                    → Danh sách phân trang + AI cá nhân hóa.
///   API 2 — GET    /api/v1/menu-items/{id}               → Chi tiết món + ghi VIEW activity.
///   API 3 — POST   /api/v1/menu-items                    → Tạo món mới (MANAGER).
///   API 4 — PATCH  /api/v1/menu-items/{id}               → Cập nhật thông tin / bật-tắt (MANAGER, CHEF).
///   API 5 — DELETE /api/v1/menu-items/{id}               → Xóa mềm (MANAGER).
///   API 6 — GET    /api/v1/menu-items/ai-recommendations → Gợi ý AI (CUSTOMER, GUEST).
///
/// Authentication:
///   - API 1, 2: Public — ai cũng truy cập được, nhưng nếu có token thì
///     Backend dùng customerId để cá nhân hóa + ghi activity.
///   - API 3, 5: [Authorize(Roles = Roles.Manager)] — chỉ quản lý.
///   - API 4: [Authorize(Roles = Roles.ManagerAndChef)] — quản lý hoặc đầu bếp.
///   - API 6: [Authorize(Roles = Roles.AllDiners)] — khách hàng.
/// </summary>
[ApiController]
[Route("api/v1/menu-items")]
public class MenuItemsController : ControllerBase
{
    private readonly MenuService _menuService;

    public MenuItemsController(MenuService menuService)
    {
        _menuService = menuService;
    }

    // ═══════════════════════════════════════════════════════════════
    // API 1: GET /api/v1/menu-items?category_id=2&search=lẩu&page=1&limit=10
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Lấy danh sách thực đơn phân trang, lọc theo danh mục.
    /// Nếu request gửi kèm token của Khách hàng thành viên,
    /// Backend ngầm cá nhân hóa thứ tự hiển thị (ưu tiên món hợp khẩu vị).
    ///
    /// Luồng dữ liệu:
    ///   Request → Controller (extract JWT nếu có) → MenuService.GetPagedAsync()
    ///     → MenuItemRepository.GetPagedFilteredAsync() → DB query (WHERE + OFFSET/LIMIT)
    ///     → (nếu có customerId) PersonalizeOrderAsync() → reorder kết quả
    ///   → PaginatedApiResponse → Client.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery(Name = "category_id")] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        if (page < 1) page = 1;
        if (limit < 1) limit = 10;
        if (limit > 50) limit = 50;

        var customerId = GetCustomerIdFromToken();

        var (items, totalCount, totalPages) = await _menuService.GetPagedAsync(
            categoryId, search, page, limit, customerId);

        return Ok(PaginatedApiResponse<MenuItemSummaryResponse>.Ok(items, totalCount, page, totalPages));
    }

    // ═══════════════════════════════════════════════════════════════
    // API 6: GET /api/v1/menu-items/ai-recommendations?limit=5
    // (Đặt TRƯỚC route {id:int} để tránh conflict routing)
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Gợi ý món ngon từ AI — cổng API chuyên dụng cho Carousel "Món ngon gợi ý cho bạn".
    /// Model AI quét business_context_logs + customer_activities để đề xuất tối ưu.
    ///
    /// Luồng dữ liệu:
    ///   Request (JWT token) → Controller (extract customerId)
    ///     → MenuService.GetAiRecommendationsAsync()
    ///       → BusinessContextLogs (thời tiết, ngày lễ, cuối tuần)
    ///       → CustomerActivities (VIEW, ORDER history)
    ///       → Rule-based engine → merge + dedup
    ///       → Ghi RecommendationLog (tracking AI performance)
    ///   → AiRecommendationResponse { recommendation_id, data[] } → Client.
    /// </summary>
    [HttpGet("ai-recommendations")]
    [Authorize(Roles = Roles.AllDiners)]
    public async Task<IActionResult> GetAiRecommendations([FromQuery] int limit = 5)
    {
        if (limit < 1) limit = 1;
        if (limit > 20) limit = 20;

        var customerId = GetCustomerIdFromToken();
        var result = await _menuService.GetAiRecommendationsAsync(customerId, limit);
        return Ok(ApiResponse<AiRecommendationResponse>.Ok(result));
    }

    // ═══════════════════════════════════════════════════════════════
    // API 2: GET /api/v1/menu-items/{id}
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Xem chi tiết thông tin món ăn kèm đánh giá (Reviews).
    /// TRIGGER ẨN: hệ thống tự động ghi 1 hành động VIEW vào customer_activities
    /// và tăng total_views trong menu_item_statistics.
    ///
    /// Luồng dữ liệu:
    ///   Request → Controller (extract customerId nếu có token)
    ///     → MenuService.GetByIdDetailAsync()
    ///       → MenuItemRepository.GetByIdWithDetailsAsync() → Include(Category, Statistics, Reviews.Customer)
    ///       → TrackViewActivityAsync() → INSERT customer_activities + UPDATE menu_item_statistics
    ///     → MenuItemDetailResponse { id, name, price, average_rating, total_views, reviews[] }
    ///   → ApiResponse → Client.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customerId = GetCustomerIdFromToken();
        var result = await _menuService.GetByIdDetailAsync(id, customerId);
        return Ok(ApiResponse<MenuItemDetailResponse>.Ok(result));
    }

    // ═══════════════════════════════════════════════════════════════
    // API 3: POST /api/v1/menu-items
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Quản lý thêm món ăn mới vào thực đơn.
    ///
    /// Luồng dữ liệu:
    ///   Request (JWT MANAGER) + Body { category_id, name, description, price, image_url }
    ///     → Controller → MenuService.CreateAsync()
    ///       → Validate (name required, price > 0)
    ///       → INSERT menu_items + INSERT menu_item_statistics (initial)
    ///     → MenuItemCreatedResponse { id, name, created_at }
    ///   → ApiResponse (201 Created) → Client.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Create([FromBody] CreateMenuItemRequest request)
    {
        var result = await _menuService.CreateAsync(request);
        return Created("", ApiResponse<MenuItemCreatedResponse>.Ok(result, ValidationMessages.MENU_ITEM_CREATED_SUCCESS));
    }

    // ═══════════════════════════════════════════════════════════════
    // API 4: PATCH /api/v1/menu-items/{id}
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Cập nhật thông tin món hoặc bật/tắt nhanh trạng thái is_available.
    /// Partial update (PATCH): chỉ gửi field cần thay đổi.
    /// Đầu bếp dùng trên iPad khi hết nguyên liệu đột ngột.
    ///
    /// Luồng dữ liệu:
    ///   Request (JWT MANAGER/CHEF) + Body { is_available: false }
    ///     → Controller → MenuService.PatchAsync()
    ///       → Load entity → apply chỉ các field non-null từ request → SaveChanges
    ///     → MenuItemUpdatedResponse { id, name, is_available, updated_at }
    ///   → ApiResponse → Client.
    /// </summary>
    [HttpPatch("{id:int}")]
    [Authorize(Roles = Roles.ManagerAndChef)]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchMenuItemRequest request)
    {
        var result = await _menuService.PatchAsync(id, request);
        return Ok(ApiResponse<MenuItemUpdatedResponse>.Ok(result, ValidationMessages.MENU_ITEM_UPDATED_SUCCESS));
    }

    // ═══════════════════════════════════════════════════════════════
    // API 5: DELETE /api/v1/menu-items/{id}
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Xóa mềm món ăn khỏi thực đơn.
    /// Giữ lại trong DB để không gãy dữ liệu lịch sử đơn hàng cũ.
    ///
    /// Luồng dữ liệu:
    ///   Request (JWT MANAGER) + Param id
    ///     → Controller → MenuService.DeleteAsync()
    ///       → Validate tồn tại → SET IsDeleted=true, UpdatedAt=UtcNow
    ///     → ApiResponse { success, message } → Client.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> Delete(int id)
    {
        await _menuService.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, ValidationMessages.MENU_ITEM_DELETED_SUCCESS));
    }

    // ═══════════════════════════════════════════════════════════════
    // Helper
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Extract customerId từ JWT token nếu user đã authenticate và role là CUSTOMER.
    /// Trả null nếu anonymous hoặc không phải CUSTOMER.
    /// </summary>
    private int? GetCustomerIdFromToken()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;

        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != nameof(UserRole.CUSTOMER))
            return null;

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idClaim, out var id) ? id : null;
    }
}
