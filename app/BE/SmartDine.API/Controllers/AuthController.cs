using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.Services;

namespace SmartDine.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>POST /api/v1/auth/login — User or customer login</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<TokenResponse>.Ok(result, ValidationMessages.AUTH_LOGIN_SUCCESS));
    }

    /// <summary>POST /api/v1/auth/register — Register a new customer account</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Created("", ApiResponse<TokenResponse>.Ok(result, ValidationMessages.AUTH_REGISTER_SUCCESS));
    }

    /// <summary>GET /api/v1/auth/me — Get current authenticated user info</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var result = await _authService.GetCurrentUserAsync(userId, role);
        return Ok(ApiResponse<UserInfoResponse>.Ok(result));
    }

    /// <summary>POST /api/v1/auth/change-password — Change password for authenticated user</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role   = User.FindFirstValue(ClaimTypes.Role)!;
        await _authService.ChangePasswordAsync(userId, role, request);
        return Ok(ApiResponse<object>.Ok(null, ValidationMessages.AUTH_CHANGE_PASSWORD_SUCCESS));
    }

    /// <summary>POST /api/v1/auth/forgot-password — Request a password reset token</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(result));
    }

    /// <summary>POST /api/v1/auth/reset-password — Reset password using a valid reset token</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordAsync(request);
        return Ok(ApiResponse<object>.Ok(null, ValidationMessages.AUTH_RESET_PASSWORD_SUCCESS));
    }
}
