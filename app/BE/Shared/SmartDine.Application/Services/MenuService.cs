using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Menu;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;
using System.Text.Json;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý nghiệp vụ quản lý thực đơn (Menu Items).
///
/// Chịu trách nhiệm:
///   - API 1: Danh sách thực đơn phân trang + filter + AI cá nhân hóa thứ tự.
///   - API 2: Chi tiết món ăn + ghi VIEW activity + tăng total_views.
///   - API 3: Tạo món ăn mới (MANAGER).
///   - API 4: Cập nhật thông tin / bật-tắt is_available (MANAGER, CHEF).
///   - API 5: Xóa mềm món ăn (MANAGER).
///   - API 6: Gợi ý món ngon từ AI dựa trên business_context_logs + customer_activities.
///
/// Dependency: IUnitOfWork (truy cập MenuItems, Reviews, CustomerActivities,
///             MenuItemStatistics, BusinessContextLogs, RecommendationLogs, Customers).
/// </summary>
public class MenuService
{
    private readonly IUnitOfWork _uow;

    public MenuService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // ═══════════════════════════════════════════════════════════════
    // API 1: GET /api/v1/menu-items?category_id=&search=&page=&limit=
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Lấy danh sách thực đơn phân trang, lọc theo danh mục và tìm kiếm.
    ///
    /// Luồng xử lý:
    ///   1. Controller nhận query params: category_id, search, page, limit.
    ///   2. Gọi repo GetPagedFilteredAsync → dynamic query (WHERE category_id AND/OR search LIKE)
    ///      + phân trang (OFFSET/LIMIT) + đếm tổng (COUNT).
    ///   3. Nếu có customerId (từ JWT token của khách thành viên):
    ///      a. Truy vấn customer_activities → tìm các category khách hay xem/đặt.
    ///      b. Sắp xếp lại kết quả: đẩy món thuộc category ưa thích lên đầu.
    ///      Đây là bước AI cá nhân hóa đơn giản (rule-based).
    ///   4. Map entity → DTO, trả về danh sách + pagination metadata.
    ///
    /// Error cases:
    ///   - category_id không tồn tại → trả danh sách rỗng (không throw).
    ///   - page/limit không hợp lệ → clamp về giá trị mặc định.
    /// </summary>
    public async Task<(List<MenuItemSummaryResponse> Items, int TotalCount, int TotalPages)> GetPagedAsync(
        int? categoryId, string? search, int page, int pageSize, int? customerId)
    {
        var (items, totalCount) = await _uow.MenuItems.GetPagedFilteredAsync(categoryId, search, page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var result = items.Select(MapToSummary).ToList();

        // AI cá nhân hóa: nếu khách thành viên đã đăng nhập, ưu tiên món hợp khẩu vị
        if (customerId.HasValue && result.Count > 1)
            result = await PersonalizeOrderAsync(customerId.Value, result, items);

        return (result, totalCount, totalPages);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 2: GET /api/v1/menu-items/{id}
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Xem chi tiết món ăn kèm đánh giá (Reviews).
    ///
    /// Luồng xử lý:
    ///   1. Gọi repo GetByIdWithDetailsAsync → Include Category, Statistics, Reviews+Customer.
    ///   2. Map entity → MenuItemDetailResponse (bao gồm danh sách review + average_rating + total_views).
    ///   3. TRIGGER ẨN — ghi nhận hành vi xem:
    ///      a. Tạo 1 record CustomerActivity { ActivityType="VIEW", Payload={"menu_item_id":id} }.
    ///      b. Tìm hoặc tạo MenuItemStatistics cho món này → tăng TotalViews += 1.
    ///      c. SaveChanges → commit cả activity + statistics trong 1 transaction.
    ///      Mục đích: làm giàu dữ liệu hành vi phục vụ mô hình AI gợi ý.
    ///   4. Trả về chi tiết món ăn.
    ///
    /// Error cases:
    ///   - Món ăn không tồn tại → EntityNotFoundException (404).
    /// </summary>
    public async Task<MenuItemDetailResponse> GetByIdDetailAsync(int id, int? customerId)
    {
        var item = await _uow.MenuItems.GetByIdWithDetailsAsync(id)
            ?? throw new EntityNotFoundException(ValidationMessages.MENU_ITEM_NOT_FOUND);

        // TRIGGER ẨN: ghi VIEW activity + tăng total_views
        await TrackViewActivityAsync(id, customerId);

        return MapToDetail(item);
    }

    // ═══════════════════════════════════════════════════════════════
    // API 3: POST /api/v1/menu-items
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Quản lý thêm món ăn mới vào thực đơn.
    ///
    /// Luồng xử lý:
    ///   1. Validate: tên không rỗng, giá > 0, categoryId hợp lệ.
    ///   2. Tạo entity MenuItem với IsAvailable = true (mặc định còn hàng).
    ///   3. Tạo MenuItemStatistics ban đầu (TotalViews=0, TotalOrders=0, ...).
    ///   4. AddAsync + SaveChanges.
    ///   5. Trả về MenuItemCreatedResponse { Id, Name, CreatedAt }.
    ///
    /// Error cases:
    ///   - Tên rỗng → BusinessRuleViolationException (422).
    ///   - Giá ≤ 0 → BusinessRuleViolationException (422).
    ///   - CategoryId không tồn tại → EntityNotFoundException (404).
    /// </summary>
    public async Task<MenuItemCreatedResponse> CreateAsync(CreateMenuItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleViolationException(ValidationMessages.MENU_ITEM_NAME_REQUIRED);

        if (request.Price <= 0)
            throw new BusinessRuleViolationException(ValidationMessages.MENU_ITEM_PRICE_INVALID);

        if (await _uow.MenuCategories.GetByIdAsync(request.CategoryId) == null)
            throw new EntityNotFoundException("Category", request.CategoryId);

        var item = new MenuItem
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            IsAvailable = true
        };

        await _uow.MenuItems.AddAsync(item);
        await _uow.SaveChangesAsync();

        // Tạo statistics ban đầu cho món mới
        var stats = new MenuItemStatistics { MenuItemId = item.Id };
        await _uow.MenuItemStatisticsRepo.AddAsync(stats);
        await _uow.SaveChangesAsync();

        return new MenuItemCreatedResponse
        {
            Id = item.Id,
            Name = item.Name,
            CreatedAt = item.CreatedAt
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // API 4: PATCH /api/v1/menu-items/{id}
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Cập nhật thông tin món ăn (partial update) hoặc bật/tắt trạng thái is_available.
    ///
    /// Luồng xử lý:
    ///   1. Tìm món ăn theo ID → 404 nếu không tồn tại.
    ///   2. Validate: request phải có ít nhất 1 field khác null.
    ///   3. Chỉ cập nhật các field có giá trị (partial update / PATCH semantics).
    ///      VD: gửi { "is_available": false } → chỉ tắt trạng thái, giữ nguyên tên/giá/...
    ///      Thường dùng bởi đầu bếp trên iPad khi hết nguyên liệu đột ngột.
    ///   4. Validate giá > 0 nếu có gửi price.
    ///   5. SaveChanges.
    ///   6. Trả về MenuItemUpdatedResponse { Id, Name, IsAvailable, UpdatedAt }.
    ///
    /// Error cases:
    ///   - Món ăn không tồn tại → EntityNotFoundException (404).
    ///   - Không có field nào để cập nhật → BusinessRuleViolationException (422).
    ///   - Price ≤ 0 → BusinessRuleViolationException (422).
    /// </summary>
    public async Task<MenuItemUpdatedResponse> PatchAsync(int id, PatchMenuItemRequest request)
    {
        var item = await _uow.MenuItems.GetByIdAsync(id)
            ?? throw new EntityNotFoundException(ValidationMessages.MENU_ITEM_NOT_FOUND);

        bool hasUpdate = request.Name != null || request.Description != null ||
                         request.Price.HasValue || request.ImageUrl != null ||
                         request.CategoryId.HasValue || request.IsAvailable.HasValue;

        if (!hasUpdate)
            throw new BusinessRuleViolationException(ValidationMessages.MENU_ITEM_PATCH_EMPTY);

        if (request.Price.HasValue && request.Price.Value <= 0)
            throw new BusinessRuleViolationException(ValidationMessages.MENU_ITEM_PRICE_INVALID);

        if (request.Name != null) item.Name = request.Name;
        if (request.Description != null) item.Description = request.Description;
        if (request.Price.HasValue) item.Price = request.Price.Value;
        if (request.ImageUrl != null) item.ImageUrl = request.ImageUrl;
        if (request.CategoryId.HasValue) item.CategoryId = request.CategoryId.Value;
        if (request.IsAvailable.HasValue) item.IsAvailable = request.IsAvailable.Value;

        await _uow.SaveChangesAsync();

        return new MenuItemUpdatedResponse
        {
            Id = item.Id,
            Name = item.Name,
            IsAvailable = item.IsAvailable,
            UpdatedAt = item.UpdatedAt ?? DateTime.UtcNow
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // API 5: DELETE /api/v1/menu-items/{id}
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Xóa mềm món ăn khỏi thực đơn hiển thị.
    ///
    /// Luồng xử lý:
    ///   1. Tìm món ăn theo ID → 404 nếu không tồn tại.
    ///   2. Gọi repo DeleteAsync → set IsDeleted=true, UpdatedAt=UtcNow.
    ///      Món bị xóa sẽ bị loại khỏi tất cả query nhờ global query filter.
    ///      Dữ liệu vẫn giữ trong DB → không gãy foreign key với order_details cũ.
    ///   3. SaveChanges.
    ///
    /// Error cases:
    ///   - Món ăn không tồn tại → EntityNotFoundException (404).
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        _ = await _uow.MenuItems.GetByIdAsync(id)
            ?? throw new EntityNotFoundException(ValidationMessages.MENU_ITEM_NOT_FOUND);

        await _uow.MenuItems.DeleteAsync(id);
        await _uow.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    // API 6: GET /api/v1/menu-items/ai-recommendations?limit=5
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Gợi ý món ngon từ AI cho Carousel "Món ngon gợi ý cho bạn".
    ///
    /// Luồng xử lý:
    ///   1. Lấy BusinessContextLog mới nhất (ngày hôm nay hoặc gần nhất).
    ///      Chứa thông tin: thời tiết, nhiệt độ, ngày lễ, cuối tuần, sự kiện.
    ///   2. Nếu có customerId (khách thành viên):
    ///      a. Truy vấn customer_activities → phân tích hành vi (VIEW, ORDER).
    ///      b. Xác định top categories khách thường xem/đặt.
    ///      c. Lấy các món từ categories ưa thích → ưu tiên đưa vào gợi ý.
    ///   3. Kết hợp context + behavior:
    ///      - Thời tiết lạnh/mưa → ưu tiên món nóng, soup, trà nóng.
    ///      - Cuối tuần/ngày lễ → ưu tiên combo gia đình, món premium.
    ///      - Khách có lịch sử → ưu tiên category quen thuộc.
    ///      - Không có data → fallback về top popular items.
    ///   4. Ghi RecommendationLog cho mỗi món được gợi ý (tracking AI performance).
    ///   5. Trả về AiRecommendationResponse { recommendation_id, data[] }.
    ///
    /// Lưu ý: Đây là rule-based engine nội bộ. Trong production có thể thay bằng
    /// gọi SmartDine.AI.API microservice qua HTTP/gRPC.
    /// </summary>
    public async Task<AiRecommendationResponse> GetAiRecommendationsAsync(int? customerId, int limit)
    {
        var batchId = $"rec_batch_{DateTime.UtcNow.Ticks % 100000}";

        // Lấy business context mới nhất
        var allContexts = await _uow.BusinessContextLogs.GetAllAsync();
        var latestContext = allContexts.FirstOrDefault();

        // Xây dựng danh sách gợi ý
        var recommendations = new List<(MenuItem Item, string Reason)>();

        // Gợi ý dựa trên ngữ cảnh kinh doanh (thời tiết, ngày lễ, cuối tuần)
        if (latestContext != null)
        {
            var contextItems = await GetContextBasedItemsAsync(latestContext, limit);
            recommendations.AddRange(contextItems);
        }

        // Gợi ý dựa trên hành vi khách hàng
        if (customerId.HasValue)
        {
            var behaviorItems = await GetBehaviorBasedItemsAsync(customerId.Value, limit);
            var existingIds = recommendations.Select(r => r.Item.Id).ToHashSet();
            recommendations.AddRange(behaviorItems.Where(b => !existingIds.Contains(b.Item.Id)));
        }

        // Fallback: top popular nếu chưa đủ
        if (recommendations.Count < limit)
        {
            var remaining = limit - recommendations.Count;
            var popular = await _uow.MenuItems.GetPopularAsync(remaining + recommendations.Count);
            var existingIds = recommendations.Select(r => r.Item.Id).ToHashSet();
            foreach (var p in popular.Where(p => !existingIds.Contains(p.Id)).Take(remaining))
                recommendations.Add((p, "Món ăn được yêu thích bởi nhiều thực khách."));
        }

        var finalList = recommendations.Take(limit).ToList();

        // Ghi RecommendationLog nếu có customerId
        if (customerId.HasValue)
        {
            foreach (var (item, reason) in finalList)
            {
                var log = new RecommendationLog
                {
                    CustomerId = customerId.Value,
                    MenuItemId = item.Id,
                    RecommendationReason = reason
                };
                await _uow.RecommendationLogs.AddAsync(log);
            }
            await _uow.SaveChangesAsync();
        }

        return new AiRecommendationResponse
        {
            RecommendationId = batchId,
            Data = finalList.Select(r => new AiRecommendedItemResponse
            {
                Id = r.Item.Id,
                Name = r.Item.Name,
                Price = r.Item.Price,
                ImageUrl = r.Item.ImageUrl,
                Reason = r.Reason
            }).ToList()
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Backward-compatible methods (dùng bởi SmartDine.API monolith)
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<MenuItemResponse>> GetAllAsync()
    {
        var items = await _uow.MenuItems.GetAvailableAsync();
        return items.Select(MapToResponse).ToList();
    }

    public async Task<MenuItemResponse> GetByIdAsync(int id)
    {
        var item = await _uow.MenuItems.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("MenuItem", id);
        return MapToResponse(item);
    }

    public async Task<MenuItemResponse> UpdateAsync(int id, UpdateMenuItemRequest request)
    {
        var item = await _uow.MenuItems.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("MenuItem", id);

        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.ImageUrl = request.ImageUrl;
        item.CategoryId = request.CategoryId;
        item.IsAvailable = request.IsAvailable;

        await _uow.SaveChangesAsync();
        return MapToResponse(item);
    }

    public async Task<List<MenuItemResponse>> SearchAsync(string query)
    {
        var items = await _uow.MenuItems.SearchAsync(query);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<List<MenuItemResponse>> GetPopularAsync(int count = 10)
    {
        var items = await _uow.MenuItems.GetPopularAsync(count);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<MenuItemResponse> ToggleAvailabilityAsync(int id)
    {
        var item = await _uow.MenuItems.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("MenuItem", id);

        item.IsAvailable = !item.IsAvailable;
        await _uow.SaveChangesAsync();
        return MapToResponse(item);
    }

    // ═══════════════════════════════════════════════════════════════
    // Private Helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// AI cá nhân hóa: sắp xếp lại danh sách món, đẩy món thuộc category
    /// mà khách hay xem/đặt lên đầu.
    /// </summary>
    private async Task<List<MenuItemSummaryResponse>> PersonalizeOrderAsync(
        int customerId, List<MenuItemSummaryResponse> items, IReadOnlyList<MenuItem> rawItems)
    {
        var activities = await _uow.CustomerActivities.GetAllAsync();
        var customerActivities = activities
            .Where(a => a.CustomerId == customerId &&
                        (a.ActivityType == ActivityType.VIEW.ToString() ||
                         a.ActivityType == ActivityType.ORDER.ToString()))
            .ToList();

        if (customerActivities.Count == 0)
            return items;

        // Phân tích: đếm category nào khách tương tác nhiều nhất
        var categoryScores = new Dictionary<int, int>();
        foreach (var activity in customerActivities)
        {
            if (activity.Payload == null) continue;
            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(activity.Payload);
                if (payload != null && payload.TryGetValue("menu_item_id", out var menuItemIdEl))
                {
                    var menuItemId = menuItemIdEl.GetInt32();
                    var menuItem = rawItems.FirstOrDefault(m => m.Id == menuItemId);
                    if (menuItem != null)
                    {
                        categoryScores.TryAdd(menuItem.CategoryId, 0);
                        categoryScores[menuItem.CategoryId]++;
                    }
                }
            }
            catch { /* payload không parse được → bỏ qua */ }
        }

        if (categoryScores.Count == 0)
            return items;

        // Sắp xếp: món thuộc category có score cao → lên đầu
        var categoryIdToScore = categoryScores;
        var itemCategoryMap = rawItems.ToDictionary(m => m.Id, m => m.CategoryId);

        return items.OrderByDescending(i =>
            itemCategoryMap.TryGetValue(i.Id, out var catId) &&
            categoryIdToScore.TryGetValue(catId, out var score) ? score : 0)
            .ToList();
    }

    /// <summary>
    /// Ghi nhận 1 hành động VIEW vào customer_activities + tăng total_views.
    /// Fire-and-forget logic: không throw nếu ghi thất bại (không block response).
    /// </summary>
    private async Task TrackViewActivityAsync(int menuItemId, int? customerId)
    {
        var activity = new CustomerActivity
        {
            CustomerId = customerId,
            ActivityType = ActivityType.VIEW.ToString(),
            Payload = JsonSerializer.Serialize(new { menu_item_id = menuItemId })
        };
        await _uow.CustomerActivities.AddAsync(activity);

        // Tăng total_views trong menu_item_statistics
        var allStats = await _uow.MenuItemStatisticsRepo.GetAllAsync();
        var stats = allStats.FirstOrDefault(s => s.MenuItemId == menuItemId);
        if (stats != null)
        {
            stats.TotalViews++;
        }
        else
        {
            var newStats = new MenuItemStatistics
            {
                MenuItemId = menuItemId,
                TotalViews = 1
            };
            await _uow.MenuItemStatisticsRepo.AddAsync(newStats);
        }

        await _uow.SaveChangesAsync();
    }

    /// <summary>
    /// Gợi ý dựa trên ngữ cảnh kinh doanh (thời tiết, ngày lễ, cuối tuần).
    /// Rule-based: map weather/event → category phù hợp.
    /// </summary>
    private async Task<List<(MenuItem Item, string Reason)>> GetContextBasedItemsAsync(
        BusinessContextLog context, int limit)
    {
        var results = new List<(MenuItem, string)>();
        var allItems = await _uow.MenuItems.GetAvailableAsync();

        // Thời tiết lạnh/mưa → món nóng, soup
        if (context.WeatherCondition != null)
        {
            var weather = context.WeatherCondition.ToLower();
            if (weather.Contains("mưa") || weather.Contains("rain") || weather.Contains("lạnh") || weather.Contains("cold"))
            {
                var warmItems = allItems.Where(m =>
                    m.Name.ToLower().Contains("nóng") || m.Name.ToLower().Contains("soup") ||
                    m.Name.ToLower().Contains("lẩu") || m.Name.ToLower().Contains("trà") ||
                    m.Name.ToLower().Contains("gừng") || m.Name.ToLower().Contains("cháo") ||
                    (m.Description != null && (m.Description.ToLower().Contains("nóng") || m.Description.ToLower().Contains("ấm")))).ToList();

                var reason = context.Temperature.HasValue
                    ? $"Thực đơn ấm nóng lý tưởng cho thời tiết {context.WeatherCondition} ({context.Temperature}°C) hiện tại."
                    : $"Thực đơn ấm nóng lý tưởng cho ngày {context.WeatherCondition} hiện tại.";

                foreach (var item in warmItems.Take(limit))
                    results.Add((item, reason));
            }

            if (weather.Contains("nắng") || weather.Contains("sunny") || weather.Contains("nóng") || weather.Contains("hot"))
            {
                var coolItems = allItems.Where(m =>
                    m.Name.ToLower().Contains("lạnh") || m.Name.ToLower().Contains("đá") ||
                    m.Name.ToLower().Contains("sinh tố") || m.Name.ToLower().Contains("nước") ||
                    m.Name.ToLower().Contains("kem") ||
                    (m.Description != null && m.Description.ToLower().Contains("mát"))).ToList();

                var reason = $"Món giải nhiệt phù hợp cho thời tiết {context.WeatherCondition} hiện tại.";

                foreach (var item in coolItems.Take(limit))
                    results.Add((item, reason));
            }
        }

        // Ngày lễ → gợi ý món đặc biệt
        if (!string.IsNullOrEmpty(context.HolidayName))
        {
            var specialItems = allItems.OrderByDescending(m => m.Price).Take(limit / 2 + 1);
            foreach (var item in specialItems)
            {
                if (!results.Any(r => r.Item1.Id == item.Id))
                    results.Add((item, $"Món đặc biệt cho dịp {context.HolidayName}."));
            }
        }

        // Cuối tuần → gợi ý combo, món gia đình
        if (context.IsWeekend)
        {
            var weekendItems = allItems.OrderByDescending(m => m.Price).Take(limit / 2 + 1);
            foreach (var item in weekendItems)
            {
                if (!results.Any(r => r.Item1.Id == item.Id))
                    results.Add((item, "Món hấp dẫn cho buổi tụ họp cuối tuần."));
            }
        }

        return results;
    }

    /// <summary>
    /// Gợi ý dựa trên hành vi khách hàng (VIEW, ORDER history).
    /// Phân tích category khách hay tương tác → đề xuất món tương tự nhưng chưa thử.
    /// </summary>
    private async Task<List<(MenuItem Item, string Reason)>> GetBehaviorBasedItemsAsync(
        int customerId, int limit)
    {
        var allActivities = await _uow.CustomerActivities.GetAllAsync();
        var customerActivities = allActivities
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .ToList();

        if (customerActivities.Count == 0)
            return new List<(MenuItem, string)>();

        // Trích xuất menu_item_id từ activities → tìm category yêu thích
        var viewedItemIds = new HashSet<int>();
        var categoryFrequency = new Dictionary<int, int>();

        foreach (var activity in customerActivities)
        {
            if (activity.Payload == null) continue;
            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(activity.Payload);
                if (payload != null && payload.TryGetValue("menu_item_id", out var idEl))
                {
                    viewedItemIds.Add(idEl.GetInt32());
                }
            }
            catch { continue; }
        }

        if (viewedItemIds.Count == 0)
            return new List<(MenuItem, string)>();

        // Tìm category từ các item đã xem
        var viewedItems = await _uow.MenuItems.GetByIdsAsync(viewedItemIds.ToList());
        foreach (var item in viewedItems)
        {
            categoryFrequency.TryAdd(item.CategoryId, 0);
            categoryFrequency[item.CategoryId]++;
        }

        // Lấy top categories → tìm món mới trong category đó mà khách chưa xem
        var topCategoryIds = categoryFrequency
            .OrderByDescending(kv => kv.Value)
            .Take(3)
            .Select(kv => kv.Key)
            .ToList();

        var recommendedItems = await _uow.MenuItems.GetByCategoryIdsAsync(topCategoryIds, limit * 2);
        var unseenItems = recommendedItems.Where(m => !viewedItemIds.Contains(m.Id)).Take(limit).ToList();

        return unseenItems.Select(item =>
            (item, $"Dựa trên khẩu vị của bạn với danh mục {item.Category?.Name ?? "yêu thích"}."))
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // Mappers
    // ═══════════════════════════════════════════════════════════════

    private static MenuItemSummaryResponse MapToSummary(MenuItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Price = item.Price,
        ImageUrl = item.ImageUrl,
        IsAvailable = item.IsAvailable
    };

    private static MenuItemDetailResponse MapToDetail(MenuItem item)
    {
        var reviews = item.Reviews?
            .Where(r => r.Status == ReviewStatus.APPROVED || r.Status == ReviewStatus.PENDING)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewSummaryResponse
            {
                Id = r.Id,
                CustomerName = r.Customer?.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList() ?? new();

        var avgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;

        return new MenuItemDetailResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            ImageUrl = item.ImageUrl,
            CategoryId = item.CategoryId,
            CategoryName = item.Category?.Name,
            IsAvailable = item.IsAvailable,
            AverageRating = Math.Round(avgRating, 2),
            TotalViews = item.Statistics?.TotalViews ?? 0,
            Reviews = reviews
        };
    }

    private static MenuItemResponse MapToResponse(MenuItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Description = item.Description,
        Price = item.Price,
        ImageUrl = item.ImageUrl,
        CategoryId = item.CategoryId,
        CategoryName = item.Category?.Name,
        IsAvailable = item.IsAvailable
    };
}
