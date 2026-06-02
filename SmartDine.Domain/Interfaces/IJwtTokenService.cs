using System.Security.Claims;
using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
