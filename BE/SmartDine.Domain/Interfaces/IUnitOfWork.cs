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
    IRepository<SessionParticipant> SessionParticipants { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
