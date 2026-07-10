using System.Security.Claims;

namespace SmartDine.Domain.Interfaces;

public interface IJwtTokenService
{
    (string Token, string JwtId) GenerateAccessToken(int id, string email, string fullName, string role);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    (string Token, string JwtId) GenerateGuestToken(string guestUniqueId, int sessionId, string guestName);

    string GeneratePasswordResetToken(int id, string email, string role);
    ClaimsPrincipal? ValidatePasswordResetToken(string token);
}
