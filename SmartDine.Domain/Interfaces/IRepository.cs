using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Generic repository interface cho tất cả entities.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> GetPagedAsync(int page, int pageSize);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<int> CountAsync();
}
