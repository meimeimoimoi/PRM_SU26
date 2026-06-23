using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Security;

/// <summary>
/// Implement IPasswordHasher bằng BCrypt (BCrypt.Net-Next).
///
/// BCrypt đặc điểm:
///   - Tự sinh salt random mỗi lần hash → cùng password luôn cho hash khác nhau.
///   - Work factor (cost) mặc định = 11 → ~200ms/hash trên hardware trung bình.
///   - Chống brute-force: tăng cost lên 1 → thời gian hash tăng gấp đôi.
///   - Hash output đã bao gồm salt → không cần lưu salt riêng.
///
/// Dùng trong:
///   - Register: hash password trước khi lưu vào Customer.PasswordHash.
///   - Login: verify password plaintext với hash trong DB.
///   - Reset password: hash password mới thay thế hash cũ.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
