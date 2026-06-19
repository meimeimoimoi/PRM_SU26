using System.Security.Claims;

namespace SmartDine.Domain.Interfaces;

public interface IJwtTokenService
{
    (string token, string jwtId) GenerateAccessToken(int id, string email, string fullName, string role);
    string GenerateRefreshToken();
    string GeneratePasswordResetToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
