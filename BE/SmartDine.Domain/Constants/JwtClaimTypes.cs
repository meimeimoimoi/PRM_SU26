namespace SmartDine.Domain.Constants;

/// <summary>
/// Tên các custom claim dùng trong JWT (ngoài các claim chuẩn của ClaimTypes).
/// </summary>
public static class JwtClaimTypes
{
    /// <summary>
    /// Claim chứa DiningSession.Id thực tế. Dùng cho GUEST vì claim "sub" (NameIdentifier)
    /// của GUEST là một UUID định danh phiên đăng nhập, không phải sessionId.
    /// </summary>
    public const string SessionId = "session_id";
}
