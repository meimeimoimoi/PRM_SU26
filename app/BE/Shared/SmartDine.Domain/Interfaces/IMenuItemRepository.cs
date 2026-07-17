using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IMenuItemRepository : IRepository<MenuItem>
{
    Task<IReadOnlyList<MenuItem>> GetByCategoryIdAsync(int categoryId);
    Task<IReadOnlyList<MenuItem>> GetAvailableAsync();
    Task<IReadOnlyList<MenuItem>> SearchAsync(string query);
    Task<IReadOnlyList<MenuItem>> GetPopularAsync(int count);
    Task<IReadOnlyList<MenuItem>> GetByIdsAsync(List<int> ids);
    /// <summary>
    /// includeUnavailable: mặc định false (khách hàng chỉ thấy món còn hàng).
    /// Admin dashboard truyền true để quản lý được cả món đã tắt is_available — nếu không,
    /// món bị tắt sẽ biến mất vĩnh viễn khỏi danh sách quản lý, không có cách nào bật lại.
    /// </summary>
    Task<(IReadOnlyList<MenuItem> Items, int TotalCount)> GetPagedFilteredAsync(int? categoryId, string? search, int page, int pageSize, bool includeUnavailable = false);
    Task<MenuItem?> GetByIdWithDetailsAsync(int id);
    Task<IReadOnlyList<MenuItem>> GetByCategoryIdsAsync(List<int> categoryIds, int count);
}
