using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IOrderRepository Orders { get; }
    IMenuItemRepository MenuItems { get; }
    IUserRepository Users { get; }
    ICustomerRepository Customers { get; }
    ITableRepository Tables { get; }
    IDiningSessionRepository DiningSessions { get; }
    IPaymentRepository Payments { get; }
    IReviewRepository Reviews { get; }
    ITableReservationRepository TableReservations { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IPasswordResetTokenRepository PasswordResetTokens { get; }
    IRepository<CustomerActivity> CustomerActivities { get; }
    IRepository<MenuItemStatistics> MenuItemStatisticsRepo { get; }
    IRepository<BusinessContextLog> BusinessContextLogs { get; }
    IRepository<RecommendationLog> RecommendationLogs { get; }
    IRepository<SessionParticipant> SessionParticipants { get; }

    ICouponRepository Coupons { get; }
    IRepository<LoyaltyTransaction> LoyaltyTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
