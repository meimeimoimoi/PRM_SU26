namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Contract cho service hash mật khẩu.
/// Implement bởi BCryptPasswordHasher (Infrastructure layer).
///
/// BCrypt tự động sinh salt random cho mỗi lần hash → cùng password cho hash khác nhau.
/// Verify so sánh password plaintext với hash đã lưu trong DB.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash password plaintext → chuỗi BCrypt hash (bao gồm salt).
    /// Dùng khi: register, reset password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// So sánh password plaintext với BCrypt hash đã lưu trong DB.
    /// Dùng khi: login, xác thực mật khẩu hiện tại.
    /// </summary>
    bool VerifyPassword(string password, string hashedPassword);
}
