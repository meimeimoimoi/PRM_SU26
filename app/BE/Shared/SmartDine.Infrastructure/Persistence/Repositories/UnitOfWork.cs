using SmartDine.Domain.Entities;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SmartDineDbContext _context;

    public IOrderRepository Orders { get; }
    public IMenuItemRepository MenuItems { get; }
    public IUserRepository Users { get; }
    public ICustomerRepository Customers { get; }
    public ITableRepository Tables { get; }
    public IDiningSessionRepository DiningSessions { get; }
    public IPaymentRepository Payments { get; }
    public IReviewRepository Reviews { get; }
    public ITableReservationRepository TableReservations { get; }
    public IRefreshTokenRepository RefreshTokens { get; }
    public IPasswordResetTokenRepository PasswordResetTokens { get; }
    public IRepository<CustomerActivity> CustomerActivities { get; }
    public IRepository<MenuItemStatistics> MenuItemStatisticsRepo { get; }
    public IRepository<BusinessContextLog> BusinessContextLogs { get; }
    public IRepository<RecommendationLog> RecommendationLogs { get; }
    public IRepository<SessionParticipant> SessionParticipants { get; }

    public ICouponRepository Coupons { get; }
    public IRepository<LoyaltyTransaction> LoyaltyTransactions { get; }
    public IRepository<MenuCategory> MenuCategories { get; }
    public ISettingsRepository Settings { get; }

    public UnitOfWork(SmartDineDbContext context)
    {
        _context = context;
        Orders = new OrderRepository(context);
        MenuItems = new MenuItemRepository(context);
        Users = new UserRepository(context);
        Customers = new CustomerRepository(context);
        Tables = new TableRepository(context);
        DiningSessions = new DiningSessionRepository(context);
        Payments = new PaymentRepository(context);
        Reviews = new ReviewRepository(context);
        TableReservations = new TableReservationRepository(context);
        RefreshTokens = new RefreshTokenRepository(context);
        PasswordResetTokens = new PasswordResetTokenRepository(context);
        CustomerActivities = new GenericRepository<CustomerActivity>(context);
        MenuItemStatisticsRepo = new GenericRepository<MenuItemStatistics>(context);
        BusinessContextLogs = new GenericRepository<BusinessContextLog>(context);
        RecommendationLogs = new GenericRepository<RecommendationLog>(context);
        SessionParticipants = new GenericRepository<SessionParticipant>(context);
        Coupons = new CouponRepository(context);
        LoyaltyTransactions = new GenericRepository<LoyaltyTransaction>(context);
        MenuCategories = new GenericRepository<MenuCategory>(context);
        Settings = new SettingsRepository(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);

    public void Dispose() => _context.Dispose();
}
