using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Interfaces;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task InvalidateAllByUserAsync(int userId, UserType userType);
}
