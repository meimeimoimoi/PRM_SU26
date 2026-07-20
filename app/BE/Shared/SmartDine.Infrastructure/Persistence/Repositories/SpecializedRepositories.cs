using Microsoft.EntityFrameworkCore;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartDine.Infrastructure.Persistence.Repositories;

public class MenuItemRepository : GenericRepository<MenuItem>, IMenuItemRepository
{
    public MenuItemRepository(SmartDineDbContext context) : base(context) { }

    public async Task<IReadOnlyList<MenuItem>> GetByCategoryIdAsync(int categoryId) =>
        await _dbSet.Where(m => m.CategoryId == categoryId)
                    .OrderBy(m => m.Name).ToListAsync();

    public async Task<IReadOnlyList<MenuItem>> GetAvailableAsync() =>
        await _dbSet.Include(m => m.Category)
                    .Where(m => m.IsAvailable)
                    .OrderBy(m => m.Category.Name).ThenBy(m => m.Name)
                    .ToListAsync();

    public async Task<IReadOnlyList<MenuItem>> SearchAsync(string query) =>
        await _dbSet.Include(m => m.Category)
                    .Where(m => m.Name.ToLower().Contains(query.ToLower()) ||
                                (m.Description != null && m.Description.ToLower().Contains(query.ToLower())))
                    .ToListAsync();

    public async Task<IReadOnlyList<MenuItem>> GetPopularAsync(int count) =>
        await _dbSet.Include(m => m.Category)
                    .Include(m => m.OrderDetails)
                    .OrderByDescending(m => m.OrderDetails.Count)
                    .Take(count)
                    .ToListAsync();

    public async Task<IReadOnlyList<MenuItem>> GetByIdsAsync(List<int> ids) =>
        await _dbSet.Where(m => ids.Contains(m.Id)).ToListAsync();

    public async Task<(IReadOnlyList<MenuItem> Items, int TotalCount)> GetPagedFilteredAsync(
        int? categoryId, string? search, int page, int pageSize, bool includeUnavailable = false)
    {
        var query = _dbSet.Include(m => m.Category)
                          .Include(m => m.Statistics)
                          .AsQueryable();

        if (!includeUnavailable)
            query = query.Where(m => m.IsAvailable);

        if (categoryId.HasValue)
            query = query.Where(m => m.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Name.ToLower().Contains(search.ToLower()) ||
                                     (m.Description != null && m.Description.ToLower().Contains(search.ToLower())));

        var totalCount = await query.CountAsync();

        var items = await query.OrderBy(m => m.Category.Name).ThenBy(m => m.Name)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return (items, totalCount);
    }

    public async Task<MenuItem?> GetByIdWithDetailsAsync(int id) =>
        await _dbSet.Include(m => m.Category)
                    .Include(m => m.Statistics)
                    .Include(m => m.Reviews).ThenInclude(r => r.Customer)
                    .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IReadOnlyList<MenuItem>> GetByCategoryIdsAsync(List<int> categoryIds, int count) =>
        await _dbSet.Include(m => m.Category)
                    .Where(m => m.IsAvailable && categoryIds.Contains(m.CategoryId))
                    .OrderByDescending(m => m.OrderDetails.Count)
                    .Take(count)
                    .ToListAsync();
}

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(SmartDineDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
    }

    public async Task<bool> ExistsAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _dbSet.AnyAsync(u => u.Email.ToLower() == normalized);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedFilteredAsync(
        string? role, bool? isActive, int page, int pageSize)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, true, out var parsedRole))
            query = query.Where(u => u.Role == parsedRole);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        var items = await query.OrderBy(u => u.FullName)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return (items, totalCount);
    }
}

public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(SmartDineDbContext context) : base(context) { }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _dbSet.FirstOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == normalized);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone) =>
        await _dbSet.FirstOrDefaultAsync(c => c.Phone == phone);
}

/// <summary>
/// Repository truy vấn bàn ăn (dining_tables).
/// Kế thừa GenericRepository (CRUD + soft delete) và bổ sung các query lọc theo trạng thái, số bàn, sức chứa.
/// </summary>
public class TableRepository : GenericRepository<Table>, ITableRepository
{
    public TableRepository(SmartDineDbContext context) : base(context) { }

    /// <summary>
    /// Include Location để Service có tên khu vực mà không cần query thêm.
    /// </summary>
    public override async Task<Table?> GetByIdAsync(int id) =>
        await _dbSet.Include(t => t.Location).FirstOrDefaultAsync(t => t.Id == id);

    /// <summary>
    /// Lọc bàn theo đúng 1 trạng thái, sắp xếp theo số bàn tăng dần.
    /// SQL: SELECT * FROM dining_tables WHERE status = @status AND is_deleted = false ORDER BY table_number.
    /// </summary>
    public async Task<IReadOnlyList<Table>> GetByStatusAsync(string status)
    {
        var parsed = Enum.Parse<TableStatus>(status, true);
        return await _dbSet.Where(t => t.Status == parsed).OrderBy(t => t.TableNumber).ToListAsync();
    }

    /// <summary>
    /// Tìm bàn theo số bàn vật lý. Trả null nếu không tồn tại.
    /// Dùng khi cần map từ số bàn in trên mặt bàn → entity trong DB.
    /// </summary>
    public async Task<Table?> GetByTableNumberAsync(int tableNumber) =>
        await _dbSet.FirstOrDefaultAsync(t => t.TableNumber == tableNumber);

    /// <summary>
    /// Lọc bàn theo nhiều điều kiện tùy chọn (status + capacity tối thiểu).
    /// Dùng dynamic query: build IQueryable rồi append WHERE clause tùy theo param nào có giá trị.
    ///
    /// VD: status="AVAILABLE", capacity=4 → SELECT * FROM dining_tables
    ///     WHERE status = 'AVAILABLE' AND capacity >= 4 ORDER BY table_number.
    /// VD: cả hai null → trả tất cả bàn (không filter).
    /// </summary>
    public async Task<IReadOnlyList<Table>> GetFilteredAsync(string? status, int? capacity)
    {
        var query = _dbSet.Include(t => t.Location).AsQueryable();
        if (!string.IsNullOrEmpty(status))
        {
            var parsed = Enum.Parse<TableStatus>(status, true);
            query = query.Where(t => t.Status == parsed);
        }
        if (capacity.HasValue)
            query = query.Where(t => t.Capacity >= capacity.Value);
        return await query.OrderBy(t => t.TableNumber).ToListAsync();
    }
}

/// <summary>
/// Repository truy vấn lịch đặt bàn trước (table_reservations).
/// Kế thừa GenericRepository (CRUD + soft delete) và bổ sung query cho nghiệp vụ booking.
/// </summary>
public class TableReservationRepository : GenericRepository<TableReservation>, ITableReservationRepository
{
    public TableReservationRepository(SmartDineDbContext context) : base(context) { }

    /// <summary>
    /// Lấy lịch đặt bàn của 1 bàn cụ thể, mới nhất lên đầu.
    /// Include Customer + Table → để Service/Controller có đủ data hiển thị mà không cần query thêm.
    /// </summary>
    public async Task<IReadOnlyList<TableReservation>> GetByTableIdAsync(int tableId) =>
        await _dbSet.Include(r => r.Customer)
                    .Include(r => r.Table)
                    .Where(r => r.TableId == tableId)
                    .OrderByDescending(r => r.ReservationTime)
                    .ToListAsync();

    /// <summary>
    /// Lấy lịch đặt bàn của 1 khách hàng, mới nhất lên đầu.
    /// Include Table → để hiển thị số bàn, sức chứa bên phía client.
    /// </summary>
    public async Task<IReadOnlyList<TableReservation>> GetByCustomerIdAsync(int customerId) =>
        await _dbSet.Include(r => r.Table)
                    .Where(r => r.CustomerId == customerId)
                    .OrderByDescending(r => r.ReservationTime)
                    .ToListAsync();

    /// <summary>
    /// Kiểm tra xung đột lịch đặt bàn trong khung giờ ±2 tiếng.
    ///
    /// Logic: Tìm các reservation KHÔNG phải CANCELLED/NO_SHOW mà có ReservationTime
    /// nằm trong [reservationTime - 2h, reservationTime + 2h].
    /// Nếu trả về count > 0 → bàn đã bị trùng lịch → Service sẽ reject.
    ///
    /// Lý do chọn ±2h: trung bình 1 bữa ăn tại nhà hàng kéo dài 1.5-2 tiếng,
    /// window ±2h đảm bảo không có 2 nhóm khách overlap cùng bàn.
    /// </summary>
    public async Task<IReadOnlyList<TableReservation>> GetActiveByTableAndTimeAsync(int tableId, DateTime reservationTime)
    {
        var windowStart = reservationTime.AddHours(-2);
        var windowEnd = reservationTime.AddHours(2);
        return await _dbSet.Where(r => r.TableId == tableId
                                    && r.Status != ReservationStatus.CANCELLED
                                    && r.Status != ReservationStatus.NO_SHOW
                                    && r.ReservationTime >= windowStart
                                    && r.ReservationTime <= windowEnd)
                           .ToListAsync();
    }
}

public class DiningSessionRepository : GenericRepository<DiningSession>, IDiningSessionRepository
{
    public DiningSessionRepository(SmartDineDbContext context) : base(context) { }

    public async Task<IReadOnlyList<DiningSession>> GetActiveSessionsAsync() =>
        await _dbSet.Include(d => d.Customer)
                    .Include(d => d.Table)
                    .Where(d => d.Status == DiningSessionStatus.ACTIVE)
                    .ToListAsync();

    public async Task<DiningSession?> GetActiveByTableIdAsync(int tableId) =>
        await _dbSet.Include(d => d.Orders)
                    .Include(d => d.Participants)
                    .FirstOrDefaultAsync(d => d.TableId == tableId && d.Status == DiningSessionStatus.ACTIVE);

    public async Task<DiningSession?> GetPayableByTableIdAsync(int tableId) =>
        await _dbSet.Include(d => d.Orders)
                    .Include(d => d.Participants)
                    .FirstOrDefaultAsync(d =>
                        d.TableId == tableId &&
                        (d.Status == DiningSessionStatus.ACTIVE || d.Status == DiningSessionStatus.CHECKOUT));

    public async Task<DiningSession?> GetByIdWithParticipantsAsync(int id) =>
        await _dbSet.Include(d => d.Table)
                    .Include(d => d.Customer)
                    .Include(d => d.Participants)
                    .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<DiningSession?> GetByIdWithParticipantsAndOrdersAsync(int id) =>
        await _dbSet.Include(d => d.Table)
                    .Include(d => d.Customer)
                    .Include(d => d.Participants)
                    .Include(d => d.Orders)
                    .FirstOrDefaultAsync(d => d.Id == id);
}

public class CouponRepository : GenericRepository<CustomerCoupon>, ICouponRepository
{
    public CouponRepository(SmartDineDbContext context) : base(context) { }

    public async Task<Promotion?> GetActivePromotionByCodeAsync(string code) =>
        await _context.Set<Promotion>()
                      .FirstOrDefaultAsync(p => p.Code == code && p.IsActive);

    public async Task<CustomerCoupon?> GetByCustomerAndPromotionAsync(int customerId, int promotionId) =>
        await _dbSet.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.PromotionId == promotionId);
}

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(SmartDineDbContext context) : base(context) { }

    /// <summary>Backward-compat — lấy payment gắn với 1 Order cụ thể.</summary>
    public async Task<Payment?> GetByOrderIdAsync(int orderId) =>
        await _dbSet.FirstOrDefaultAsync(p => p.OrderId == orderId);

    /// <summary>
    /// Lấy orderCode duy nhất từ PostgreSQL sequence.
    /// Sequence đảm bảo không bao giờ trùng dù nhiều request tạo payment cùng lúc.
    /// Fallback về timestamp-based khi chạy InMemory (test): không cần sequence thật.
    /// </summary>
    public async Task<long> GetNextOrderCodeAsync()
    {
        if (_context.Database.IsInMemory())
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 9_999_999_999L;

        var conn = _context.Database.GetDbConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT nextval('payment_order_code_seq')";
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    /// <summary>
    /// Trả về payments PENDING tạo trước thời điểm cutoff.
    /// Background job dùng để detect payment hết hạn (PayOS link tồn tại 30 phút).
    /// Include Session để job có thể revert trạng thái về ACTIVE ngay trong cùng loop.
    /// </summary>
    public async Task<IReadOnlyList<Payment>> GetPendingOlderThanAsync(DateTime cutoff) =>
        await _dbSet.Include(p => p.Session)
                    .Where(p => p.PaymentStatus == PaymentStatus.PENDING && p.CreatedAt < cutoff)
                    .ToListAsync();

    /// <summary>
    /// Lấy payment mới nhất của 1 DiningSession.
    /// Dùng khi create-intent: kiểm tra PENDING/SUCCESS trước khi tạo mới.
    /// Sắp xếp DESC theo CreatedAt → lấy record mới nhất (có thể có nhiều FAILED).
    /// </summary>
    public async Task<Payment?> GetBySessionIdAsync(int sessionId) =>
        await _dbSet.Where(p => p.SessionId == sessionId)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

    public async Task<IReadOnlyList<Payment>> GetPendingCashAsync() =>
        await _dbSet.Include(p => p.Session).ThenInclude(s => s.Table)
                    .Where(p =>
                        p.PaymentStatus == PaymentStatus.PENDING &&
                        p.PaymentMethod == PaymentMethod.CASH &&
                        (p.Session.Status == DiningSessionStatus.ACTIVE ||
                         p.Session.Status == DiningSessionStatus.CHECKOUT))
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync();

    /// <summary>
    /// Tìm payment bằng ExternalRef (PayOS orderCode dạng string).
    /// Key dùng để match webhook gọi về với Payment record đã tạo.
    /// </summary>
    public async Task<Payment?> GetByExternalRefAsync(string externalRef) =>
        await _dbSet.FirstOrDefaultAsync(p => p.ExternalRef == externalRef);

    public async Task<IReadOnlyList<Payment>> GetByDateRangeAsync(DateTime start, DateTime end) =>
        await _dbSet.Where(p => p.PaidAt >= start && p.PaidAt <= end)
                    .OrderByDescending(p => p.PaidAt)
                    .ToListAsync();

    public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedFilteredAsync(
        DateTime? fromDate, DateTime? toDate, string? status, string? paymentMethod, int page, int pageSize)
    {
        var query = _dbSet.Include(p => p.Session).ThenInclude(s => s.Table)
                          .Include(p => p.Session).ThenInclude(s => s.Customer)
                          .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            query = query.Where(p => p.PaymentStatus == parsedStatus);

        if (!string.IsNullOrEmpty(paymentMethod) && Enum.TryParse<PaymentMethod>(paymentMethod, true, out var parsedMethod))
            query = query.Where(p => p.PaymentMethod == parsedMethod);

        var totalCount = await query.CountAsync();

        var items = await query.OrderByDescending(p => p.CreatedAt)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return (items, totalCount);
    }
}

public class ReviewRepository : GenericRepository<Review>, IReviewRepository
{
    public ReviewRepository(SmartDineDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Review>> GetByMenuItemIdAsync(int menuItemId) =>
        await _dbSet.Include(r => r.Customer)
                    .Where(r => r.MenuItemId == menuItemId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

    public async Task<IReadOnlyList<Review>> GetByCustomerIdAsync(int customerId) =>
        await _dbSet.Where(r => r.CustomerId == customerId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

    public async Task<double> GetAverageRatingByMenuItemIdAsync(int menuItemId)
    {
        var reviews = await _dbSet.Where(r => r.MenuItemId == menuItemId).ToListAsync();
        return reviews.Count == 0 ? 0 : reviews.Average(r => r.Rating);
    }
}

/// <summary>
/// Repository cấu hình nhà hàng — luôn thao tác trên bản ghi có Id nhỏ nhất (singleton row).
/// Tự động tạo bản ghi mặc định nếu bảng rỗng.
/// </summary>
public class SettingsRepository : GenericRepository<RestaurantSettings>, ISettingsRepository
{
    public SettingsRepository(SmartDineDbContext context) : base(context) { }

    public async Task<RestaurantSettings> GetSingletonAsync()
    {
        var settings = await _dbSet.OrderBy(s => s.Id).FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new RestaurantSettings
            {
                RestaurantName = "SmartDine Restaurant",
                OpeningTime = new TimeSpan(8, 0, 0),
                ClosingTime = new TimeSpan(22, 0, 0),
                TaxRate = 8.00m,
                ServiceChargeRate = 5.00m
            };
            await _dbSet.AddAsync(settings);
            await _context.SaveChangesAsync();
        }
        return settings;
    }
}
