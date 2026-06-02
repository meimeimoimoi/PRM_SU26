using Microsoft.EntityFrameworkCore;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Persistence.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(SmartDineDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, int page, int pageSize) =>
        await _dbSet.Include(o => o.Items).ThenInclude(i => i.MenuItem)
                    .Where(o => o.CustomerId == customerId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .ToListAsync();

    public async Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status) =>
        await _dbSet.Include(o => o.Items).ThenInclude(i => i.MenuItem)
                    .Include(o => o.Table)
                    .Where(o => o.Status == status)
                    .OrderBy(o => o.CreatedAt)
                    .ToListAsync();

    public async Task<IReadOnlyList<Order>> GetActiveOrdersAsync() =>
        await _dbSet.Include(o => o.Items).ThenInclude(i => i.MenuItem)
                    .Include(o => o.Table)
                    .Include(o => o.Customer)
                    .Where(o => o.Status != OrderStatus.COMPLETED && o.Status != OrderStatus.CANCELLED)
                    .OrderBy(o => o.CreatedAt)
                    .ToListAsync();

    public async Task<IReadOnlyList<Order>> GetTodayOrdersAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _dbSet.Include(o => o.Items).ThenInclude(i => i.MenuItem)
                           .Where(o => o.CreatedAt >= today)
                           .OrderByDescending(o => o.CreatedAt)
                           .ToListAsync();
    }

    public async Task<IReadOnlyList<Order>> GetByDiningSessionIdAsync(Guid sessionId) =>
        await _dbSet.Include(o => o.Items).ThenInclude(i => i.MenuItem)
                    .Where(o => o.DiningSessionId == sessionId)
                    .OrderBy(o => o.CreatedAt)
                    .ToListAsync();
}
