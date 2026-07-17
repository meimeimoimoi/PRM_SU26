using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IDiningSessionRepository : IRepository<DiningSession>
{
    Task<IReadOnlyList<DiningSession>> GetActiveSessionsAsync();
    Task<DiningSession?> GetActiveByTableIdAsync(int tableId);

    /// <summary>
    /// Phiên đang mở để thanh toán: ACTIVE hoặc CHECKOUT (khách đã tạo intent tiền mặt).
    /// Không dùng cho guest join — join chỉ lấy ACTIVE qua GetActiveByTableIdAsync.
    /// </summary>
    Task<DiningSession?> GetPayableByTableIdAsync(int tableId);

    Task<DiningSession?> GetByIdWithParticipantsAsync(int id);

    Task<DiningSession?> GetByIdWithParticipantsAndOrdersAsync(int id);
}
