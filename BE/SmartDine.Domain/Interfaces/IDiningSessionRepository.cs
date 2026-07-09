using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IDiningSessionRepository : IRepository<DiningSession>
{
    Task<IReadOnlyList<DiningSession>> GetActiveSessionsAsync();
    Task<DiningSession?> GetActiveByTableIdAsync(int tableId);
    Task<DiningSession?> GetByIdWithParticipantsAsync(int id);

    /// <summary>
    /// Load đầy đủ session: Table + Customer + Participants + Orders (kèm OrderDetails).
    /// Dùng trong create-intent: cần Participants để IDOR check, Orders để tính tổng hóa đơn.
    /// </summary>
    Task<DiningSession?> GetByIdWithParticipantsAndOrdersAsync(int id);
}
