using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IMenuItemRepository : IRepository<MenuItem>
{
    Task<IReadOnlyList<MenuItem>> GetByCategoryIdAsync(Guid categoryId);
    Task<IReadOnlyList<MenuItem>> GetAvailableAsync();
    Task<IReadOnlyList<MenuItem>> SearchAsync(string query);
    Task<IReadOnlyList<MenuItem>> GetPopularAsync(int count);
    Task<IReadOnlyList<MenuItem>> GetByIdsAsync(List<Guid> ids);
}
