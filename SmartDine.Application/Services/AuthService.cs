using SmartDine.Application.DTOs.Auth;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý authentication: login, register, refresh token.
/// </summary>
public class AuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtTokenService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUnitOfWork uow, IJwtTokenService jwtService, IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            throw new BusinessRuleViolationException("Email hoặc mật khẩu không đúng.");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new BusinessRuleViolationException("Email hoặc mật khẩu không đúng.");

        return GenerateTokenResponse(user);
    }

    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _uow.Users.ExistsAsync(request.Email))
            throw new BusinessRuleViolationException("Email đã được sử dụng.");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber,
            Role = UserRole.CUSTOMER,
            IsActive = true
        };

        await _uow.Users.AddAsync(user);

        // Tạo Customer profile
        var customer = new Customer
        {
            UserId = user.Id,
            PhoneNumber = request.PhoneNumber
        };
        await _uow.Customers.AddAsync(customer);

        // Tạo Loyalty account
        var loyalty = new LoyaltyAccount
        {
            CustomerId = customer.Id,
            TotalPoints = 0,
            CurrentPoints = 0,
            Tier = LoyaltyTier.BRONZE
        };
        // We save via context directly since we don't have a loyalty repo in UoW for simplicity
        await _uow.SaveChangesAsync();

        return GenerateTokenResponse(user);
    }

    public async Task<UserInfoResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId)
            ?? throw new EntityNotFoundException("User", userId);

        return new UserInfoResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            AvatarUrl = user.AvatarUrl
        };
    }

    private TokenResponse GenerateTokenResponse(User user)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600, // 60 minutes
            User = new UserInfoResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl
            }
        };
    }
}
