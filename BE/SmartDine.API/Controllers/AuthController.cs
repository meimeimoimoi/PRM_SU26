using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.Services;
using SmartDine.Domain.Constants;
using SmartDine.Domain.Enums;

namespace SmartDine.API.Controllers;

/// <summary>
/// Controller xác thực (Monolith API version).
/// Chức năng tương tự Identity.API nhưng không có login-guest và logout
/// (các endpoint đó chỉ có trong Identity microservice).
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

    /// <summary>POST /api/v1/auth/login — Đăng nhập (User hoặc Customer).</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, ValidationMessages.LOGIN_SUCCESS));
    }

    /// <summary>POST /api/v1/auth/register — Đăng ký Customer mới.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Created("", ApiResponse<TokenResponse>.Ok(result, ValidationMessages.REGISTER_SUCCESS));
    }

    /// <summary>POST /api/v1/auth/refresh-token — Làm mới cặp token.</summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, ValidationMessages.REFRESH_TOKEN_SUCCESS));
    }

    /// <summary>POST /api/v1/auth/forgot-password — Khởi tạo reset password flow.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(result));
    }

    /// <summary>POST /api/v1/auth/reset-password — Đặt lại mật khẩu bằng reset token.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<object>.Ok(null!, ValidationMessages.RESET_PASSWORD_SUCCESS));
    }

    /// <summary>GET /api/v1/auth/me — Thông tin user hiện tại.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        // GUEST: sub (NameIdentifier) là UUID định danh phiên đăng nhập, không phải số
        // → phải lấy sessionId từ custom claim "session_id" thay vì int.Parse(sub).
        var rawId = role == UserRole.GUEST.ToString()
            ? User.FindFirstValue(JwtClaimTypes.SessionId)!
            : User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userId = int.Parse(rawId);

        var result = await _authService.GetCurrentUserAsync(userId, role);
        return Ok(ApiResponse<UserInfoResponse>.Ok(result));
    }
}
