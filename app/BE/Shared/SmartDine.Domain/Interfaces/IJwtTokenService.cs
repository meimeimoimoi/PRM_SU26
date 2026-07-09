using System.Security.Claims;

namespace SmartDine.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(int id, string email, string fullName, string role);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    // Reset password — token ngắn hạn 15 phút, có claim purpose=password-reset
    string GeneratePasswordResetToken(int id, string email, string role);
    ClaimsPrincipal? ValidatePasswordResetToken(string token);
}
