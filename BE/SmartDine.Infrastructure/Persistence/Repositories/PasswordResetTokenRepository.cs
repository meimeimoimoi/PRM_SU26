using Microsoft.EntityFrameworkCore;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository : GenericRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(SmartDineDbContext context) : base(context) { }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token) =>
        await _dbSet.FirstOrDefaultAsync(p => p.Token == token && !p.IsUsed && p.ExpiresAt > DateTime.UtcNow);

    public async Task InvalidateAllByUserAsync(int userId, string userType)
    {
        var tokens = await _dbSet.Where(p => p.UserId == userId && p.UserType == userType && !p.IsUsed).ToListAsync();
        foreach (var token in tokens)
        {
            token.IsUsed = true;
        }
    }
}
