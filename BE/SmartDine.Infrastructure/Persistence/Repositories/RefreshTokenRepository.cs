using Microsoft.EntityFrameworkCore;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(SmartDineDbContext context) : base(context) { }

    public async Task<RefreshToken?> GetByTokenAsync(string token) =>
        await _dbSet.FirstOrDefaultAsync(r => r.Token == token && !r.IsRevoked);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(int userId, UserType userType) =>
        await _dbSet.Where(r => r.UserId == userId && r.UserType == userType && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();

    public async Task RevokeAllByUserAsync(int userId, UserType userType)
    {
        var tokens = await _dbSet.Where(r => r.UserId == userId && r.UserType == userType && !r.IsRevoked).ToListAsync();
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
    }
}
