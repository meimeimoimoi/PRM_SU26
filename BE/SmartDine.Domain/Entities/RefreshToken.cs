using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Entity lưu trữ refresh token trong DB (refresh_tokens).
///
/// Cơ chế Token Rotation:
///   1. User login → server tạo AccessToken (JWT, 1h) + RefreshToken (random 64 bytes, 7 ngày).
///   2. RefreshToken được lưu vào DB kèm JwtId (liên kết với AccessToken qua claim jti).
///   3. Khi AccessToken hết hạn → client gửi cả AccessToken (expired) + RefreshToken.
///   4. Server validate: parse expired token lấy jti → tìm RefreshToken trong DB → so khớp JwtId.
///   5. Revoke RefreshToken cũ (IsRevoked=true) → tạo cặp token mới → ghi ReplacedByToken.
///
/// ReplacedByToken tạo thành token chain: nếu phát hiện token cũ bị reuse
/// (đã revoke nhưng vẫn bị gửi lại) → có thể revoke toàn bộ chain (chống token theft).
///
/// UserType phân biệt: "USER" (nhân viên) vs "CUSTOMER" (khách hàng)
/// vì 2 bảng users/customers có ID riêng biệt.
/// </summary>
public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public UserType UserType { get; set; } = Enums.UserType.USER;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
}
