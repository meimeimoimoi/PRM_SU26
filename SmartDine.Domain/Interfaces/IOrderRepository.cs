using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, int page, int pageSize);
    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status);
    Task<IReadOnlyList<Order>> GetActiveOrdersAsync();
    Task<IReadOnlyList<Order>> GetTodayOrdersAsync();
    Task<IReadOnlyList<Order>> GetByDiningSessionIdAsync(Guid sessionId);
}
