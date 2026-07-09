using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(int userId, UserType userType);
    Task RevokeAllByUserAsync(int userId, UserType userType);
}
