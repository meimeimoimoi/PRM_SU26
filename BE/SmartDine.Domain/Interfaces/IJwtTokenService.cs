using System.Security.Claims;

namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Contract cho service quản lý JWT token.
/// Implement bởi JwtTokenService (Infrastructure layer) sử dụng RSA256 asymmetric signing.
///
/// Kiến trúc RSA256 trong microservice:
///   - Identity.API giữ PRIVATE key → ký (sign) token.
///   - Các service khác (Menu, Order, Table, AI) chỉ cần PUBLIC key → xác thực (verify) token.
///   - Lợi ích: service bị compromise không thể giả mạo token, chỉ Identity mới ký được.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Tạo JWT AccessToken chứa claims (jti, userId, email, fullName, role).
    /// Trả về tuple (token string, jwtId) — jwtId được lưu vào RefreshToken.JwtId để liên kết cặp token.
    /// Token có thời hạn mặc định 60 phút (config: Jwt:AccessTokenExpiryMinutes).
    /// </summary>
    (string token, string jwtId) GenerateAccessToken(int id, string email, string fullName, string role);

    /// <summary>
    /// Tạo refresh token: random 64 bytes → base64 string.
    /// Không phải JWT, chỉ là chuỗi ngẫu nhiên lưu trong DB.
    /// Thời hạn 7 ngày (set bởi AuthService khi lưu vào DB).
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Tạo password reset token: random 32 bytes → base64 string.
    /// Ngắn hơn refresh token vì chỉ dùng 1 lần và hết hạn sau 15 phút.
    /// </summary>
    string GeneratePasswordResetToken();

    /// <summary>
    /// Parse JWT đã hết hạn để lấy claims (userId, email, role...).
    /// ValidateLifetime = false → cho phép đọc token expired.
    /// Vẫn validate signature RSA256 → đảm bảo token do hệ thống ký, không bị giả mạo.
    /// Trả null nếu token bị tamper hoặc không phải RSA256.
    /// Dùng trong flow refresh token: cần lấy jti từ expired access token để match với refresh token trong DB.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
