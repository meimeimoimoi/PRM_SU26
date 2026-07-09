using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IMenuItemRepository : IRepository<MenuItem>
{
    Task<IReadOnlyList<MenuItem>> GetByCategoryIdAsync(int categoryId);
    Task<IReadOnlyList<MenuItem>> GetAvailableAsync();
    Task<IReadOnlyList<MenuItem>> SearchAsync(string query);
    Task<IReadOnlyList<MenuItem>> GetPopularAsync(int count);
    Task<IReadOnlyList<MenuItem>> GetByIdsAsync(List<int> ids);
    Task<(IReadOnlyList<MenuItem> Items, int TotalCount)> GetPagedFilteredAsync(int? categoryId, string? search, int page, int pageSize);
    Task<MenuItem?> GetByIdWithDetailsAsync(int id);
    Task<IReadOnlyList<MenuItem>> GetByCategoryIdsAsync(List<int> categoryIds, int count);
}
