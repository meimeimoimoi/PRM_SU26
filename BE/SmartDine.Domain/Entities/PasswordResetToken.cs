using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Entity lưu token đặt lại mật khẩu (password_reset_tokens).
///
/// Luồng forgot password:
///   1. User gửi email → server tìm User/Customer theo email.
///   2. Invalidate tất cả reset token cũ của user này (tránh dùng lại).
///   3. Tạo token mới (random 32 bytes, base64) với thời hạn 15 phút.
///   4. Trả token về client (thực tế sẽ gửi qua email, hiện tại trả trực tiếp trong response).
///   5. User gửi token + mật khẩu mới → server validate token chưa hết hạn + chưa dùng.
///   6. Cập nhật PasswordHash mới + đánh dấu IsUsed=true + revoke tất cả RefreshToken.
///
/// Revoke RefreshToken khi đổi mật khẩu: đảm bảo tất cả session cũ bị logout,
/// buộc user login lại với mật khẩu mới (bảo mật khi bị lộ mật khẩu cũ).
/// </summary>
public class PasswordResetToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserType UserType { get; set; } = Enums.UserType.CUSTOMER;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
}
