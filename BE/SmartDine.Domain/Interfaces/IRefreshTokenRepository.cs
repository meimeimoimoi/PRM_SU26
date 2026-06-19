using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(int userId, string userType);
    Task RevokeAllByUserAsync(int userId, string userType);
}
