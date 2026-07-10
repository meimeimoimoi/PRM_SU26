using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsAsync(string email);

    /// <summary>
    /// Lấy danh sách nhân viên có phân trang, lọc theo role và trạng thái active.
    /// Dùng cho manager dashboard quản lý nhân viên.
    /// </summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedFilteredAsync(string? role, bool? isActive, int page, int pageSize);
}
