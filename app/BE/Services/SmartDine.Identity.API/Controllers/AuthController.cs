using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Domain.Constants;
using SmartDine.Domain.Enums;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.Services;

namespace SmartDine.Identity.API.Controllers;

/// <summary>
/// Controller xác thực và quản lý phiên đăng nhập (Identity Microservice).
///
/// Tất cả endpoint public trừ /logout và /me (yêu cầu JWT Bearer token).
/// Luồng request: Client → Gateway(:5000) → Identity.API(:5001) → AuthController → AuthService.
/// Exception được bắt bởi ExceptionHandlingMiddleware → trả về ApiResponse chuẩn.
///
/// Đây là service DUY NHẤT giữ RSA private key để ký JWT.
/// Các service khác (Menu, Order, Table, AI) chỉ cần public key để verify.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// POST /api/v1/auth/login — Đăng nhập (User hoặc Customer).
    /// Public endpoint. Trả về cặp AccessToken (1h) + RefreshToken (7 ngày).
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, ValidationMessages.LOGIN_SUCCESS));
    }

    /// <summary>
    /// POST /api/v1/auth/register — Đăng ký tài khoản Customer mới.
    /// Public endpoint. Tự động login sau khi tạo thành công (trả TokenResponse).
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Created("", ApiResponse<TokenResponse>.Ok(result, ValidationMessages.REGISTER_SUCCESS));
    }

    /// <summary>
    /// POST /api/v1/auth/refresh-token — Làm mới cặp token khi AccessToken hết hạn.
    /// Public endpoint. Client gửi expired AccessToken + RefreshToken → nhận cặp mới.
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, ValidationMessages.REFRESH_TOKEN_SUCCESS));
    }

    /// <summary>
    /// POST /api/v1/auth/forgot-password — Khởi tạo flow đặt lại mật khẩu.
    /// Public endpoint. Trả reset token (dev mode) — production sẽ gửi qua email.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(result));
    }

    /// <summary>
    /// POST /api/v1/auth/reset-password — Đặt lại mật khẩu bằng reset token.
    /// Public endpoint. Sau reset → tất cả RefreshToken bị revoke (force re-login).
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<object>.Ok(null!, ValidationMessages.RESET_PASSWORD_SUCCESS));
    }

    /// <summary>
    /// POST /api/v1/auth/login-guest — Đăng nhập khách vãng lai.
    /// Public endpoint. Không cần tài khoản, chỉ cần TableId. Trả JWT với role=GUEST.
    /// </summary>
    [HttpPost("login-guest")]
    public async Task<IActionResult> LoginGuest([FromBody] GuestLoginRequest request)
    {
        var result = await _authService.LoginGuestAsync(request);
        return Ok(ApiResponse<GuestLoginResponse>.Ok(result, ValidationMessages.GUEST_LOGIN_SUCCESS));
    }

    /// <summary>
    /// POST /api/v1/auth/logout — Đăng xuất, revoke tất cả RefreshToken.
    /// Yêu cầu JWT. Parse userId + role từ token để xác định user cần logout.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var (userId, role) = ExtractIdentity();
        var userType = role == UserRole.CUSTOMER.ToString() ? UserType.CUSTOMER
                     : role == UserRole.GUEST.ToString() ? UserType.GUEST
                     : UserType.USER;
        var result = await _authService.LogoutAsync(userId, userType);
        return Ok(ApiResponse<LogoutResponse>.Ok(result));
    }

    /// <summary>
    /// GET /api/v1/auth/me — Lấy thông tin user hiện tại từ JWT.
    /// Yêu cầu JWT. Parse userId + role → query DB → trả UserInfoResponse.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var (userId, role) = ExtractIdentity();
        var result = await _authService.GetCurrentUserAsync(userId, role);
        return Ok(ApiResponse<UserInfoResponse>.Ok(result));
    }

    /// <summary>
    /// Lấy (id, role) từ JWT claims hiện tại.
    /// GUEST có sub (NameIdentifier) là UUID định danh phiên đăng nhập, không phải số,
    /// nên id thực tế (sessionId) phải lấy từ custom claim "session_id" thay vì int.Parse(sub).
    /// </summary>
    private (int id, string role) ExtractIdentity()
    {
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var rawId = role == UserRole.GUEST.ToString()
            ? User.FindFirstValue(JwtClaimTypes.SessionId)!
            : User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return (int.Parse(rawId), role);
    }
}
