using SmartDine.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDine.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(int customerId, int page, int pageSize);
    Task<IReadOnlyList<Order>> GetByStatusAsync(string status);
    Task<IReadOnlyList<Order>> GetActiveOrdersAsync();
    Task<IReadOnlyList<Order>> GetTodayOrdersAsync();
    Task<IReadOnlyList<Order>> GetByDiningSessionIdAsync(int sessionId);
    Task<IReadOnlyList<Order>> GetByGuestSessionIdAsync(string guestSessionId, int page, int pageSize);

    /// <summary>Lấy đơn hàng theo khoảng CreatedAt — dùng để dựng chart doanh số theo giờ/tuần/tháng.</summary>
    Task<IReadOnlyList<Order>> GetByDateRangeAsync(DateTime start, DateTime end);
}
