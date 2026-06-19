using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task InvalidateAllByUserAsync(int userId, string userType);
}
