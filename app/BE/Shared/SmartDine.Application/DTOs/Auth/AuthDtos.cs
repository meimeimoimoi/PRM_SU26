using SmartDine.Domain.Enums;

namespace SmartDine.Application.DTOs.Auth;

// ─────────────────────────────────────────────────────────────
// Login
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Request body cho POST /api/v1/auth/login.
/// Hỗ trợ cả 2 loại tài khoản: User (nhân viên) và Customer (khách hàng).
/// AuthService sẽ tìm email trong bảng users trước, rồi customers.
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Số bàn (TableNumber) — chỉ dùng khi CUSTOMER đăng nhập từ app khách.
    /// &gt; 0 → sau login tạo/join DiningSession trên bàn đó.
    /// Staff/Manager bỏ trống hoặc 0.
    /// </summary>
    public int TableNumber { get; set; }
}

// ─────────────────────────────────────────────────────────────
// Register
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Request body cho POST /api/v1/auth/register.
/// Chỉ dành cho khách hàng (Customer). Nhân viên (User) được tạo bởi Manager/Admin.
/// PhoneNumber optional — nếu có sẽ check trùng trong DB.
/// </summary>
public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

// ─────────────────────────────────────────────────────────────
// Token Response
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Response trả về sau login/register/refresh thành công.
/// Client lưu AccessToken vào header "Authorization: Bearer {token}" cho các request tiếp theo.
/// RefreshToken lưu phía client (secure storage), gửi khi AccessToken hết hạn.
/// ExpiresIn tính bằng giây (3600 = 1 giờ).
/// SessionId/TableId/TableNumber có giá trị khi CUSTOMER login kèm TableNumber (tạo/join phiên bàn).
/// </summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserInfoResponse User { get; set; } = null!;

    /// <summary>DiningSession vừa tạo/join — 0 nếu không gắn bàn.</summary>
    public int SessionId { get; set; }
    public int TableId { get; set; }
    public int TableNumber { get; set; }
}

// ─────────────────────────────────────────────────────────────
// Refresh Token
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Request body cho POST /api/v1/auth/refresh-token.
/// Client gửi cả AccessToken (đã hết hạn) + RefreshToken (còn hiệu lực).
/// Server parse expired token lấy jti → match với RefreshToken trong DB → cấp cặp token mới.
/// </summary>
public class RefreshTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────
// Forgot / Reset Password
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Request body cho POST /api/v1/auth/forgot-password.
/// Server luôn trả response thành công (không tiết lộ email có tồn tại hay không — chống enumeration).
/// </summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response sau forgot-password.
/// ResetToken nullable: chỉ có giá trị khi email tồn tại (dev mode trả trực tiếp,
/// production sẽ gửi qua email và không trả token trong response).
/// </summary>
public class ForgotPasswordResponse
{
    public string Message { get; set; } = string.Empty;
    public string? ResetToken { get; set; }
}

/// <summary>
/// Request body cho POST /api/v1/auth/reset-password.
/// Token là chuỗi base64 nhận được từ forgot-password flow.
/// NewPassword và ConfirmPassword phải khớp nhau.
/// Sau reset: token bị đánh dấu IsUsed, tất cả RefreshToken bị revoke (force re-login).
/// </summary>
public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────
// User Info
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Response cho GET /api/v1/auth/me — thông tin user hiện tại.
/// Cũng dùng làm nested object trong TokenResponse.User.
/// Role: STAFF, CHEF, MANAGER (nhân viên) hoặc CUSTOMER (khách hàng).
/// </summary>
public class UserInfoResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public int? LoyaltyPoints { get; set; }
    public string? MembershipLevel { get; set; }
}

// ─────────────────────────────────────────────────────────────
// Guest Login
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Request body cho POST /api/v1/auth/login-guest.
/// Khách vãng lai không cần tài khoản — chỉ cần biết bàn nào.
/// GuestName/GuestPhone optional, dùng để ghi nhận thông tin trong DiningSession.
/// </summary>
public class GuestLoginRequest
{
    /// <summary>
    /// Tên field giữ nguyên "TableId" để không phá vỡ contract JSON hiện có (FE gửi
    /// "tableId"), nhưng giá trị thực chất là Số Bàn (TableNumber) — QR code, form nhập
    /// tay, và quét QR trong app đều lấy số in trên bàn, không phải khóa chính DB.
    /// Xem AuthService.LoginGuestAsync — tra bằng GetByTableNumberAsync, không phải GetByIdAsync.
    /// </summary>
    public int TableId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
}

/// <summary>
/// Response sau guest login.
/// Token là JWT với role=GUEST.
///   sub = UUID duy nhất cho lần đăng nhập này (phân biệt nhiều GUEST cùng bàn).
///   claim "session_id" = sessionId thực tế.
/// Client dùng token này để gọi món tại bàn (quyền hạn giới hạn hơn CUSTOMER).
/// </summary>
public class GuestLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public int TableId { get; set; }
    public int TableNumber { get; set; }
    public string Role { get; set; } = nameof(UserRole.GUEST);
    /// <summary>Tên khách nhập khi quét QR / join bàn — hiển thị trên trang tài khoản.</summary>
    public string GuestName { get; set; } = "Guest";
}

/// <summary>CUSTOMER gắn bàn sau khi đăng nhập — tạo/join DiningSession, giữ JWT thành viên.</summary>
public class JoinTableRequest
{
    /// <summary>Số bàn in trên QR (TableNumber), không phải PK.</summary>
    public int TableNumber { get; set; }
}

/// <summary>Đăng nhập/đăng ký CUSTOMER + gắn bàn trong 1 request.</summary>
public class LoginWithTableRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    /// <summary>Có thì đăng ký mới; null/empty thì chỉ login.</summary>
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}

/// <summary>Token thành viên + phiên bàn vừa gắn.</summary>
public class CustomerDiningLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserInfoResponse User { get; set; } = null!;
    public int SessionId { get; set; }
    public int TableId { get; set; }
    public int TableNumber { get; set; }
}


// ─────────────────────────────────────────────────────────────
// Logout
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Response sau POST /api/v1/auth/logout.
/// Server revoke tất cả RefreshToken của user → AccessToken hiện tại vẫn valid
/// cho đến khi hết hạn (1h), nhưng không thể refresh được nữa.
/// </summary>
public class LogoutResponse
{
    public string Message { get; set; } = string.Empty;
}