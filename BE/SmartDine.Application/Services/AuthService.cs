using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

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
        if (user != null && user.IsActive)
        {
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                throw new BusinessRuleViolationException(ValidationMessages.EMAIL_OR_PASSSWORD_INVALID);

            return await GenerateTokenResponseAsync(user.Id, user.Email, user.FullName, user.Role, "USER");
        }

        var customer = await _uow.Customers.GetByEmailAsync(request.Email);
        if (customer != null)
        {
            if (customer.PasswordHash == null || !_passwordHasher.VerifyPassword(request.Password, customer.PasswordHash))
                throw new BusinessRuleViolationException(ValidationMessages.EMAIL_OR_PASSSWORD_INVALID);

            return await GenerateTokenResponseAsync(customer.Id, customer.Email ?? string.Empty, customer.FullName ?? "Customer", "CUSTOMER", "CUSTOMER");
        }

        throw new BusinessRuleViolationException(ValidationMessages.EMAIL_OR_PASSSWORD_INVALID);
    }

    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _uow.Users.ExistsAsync(request.Email) || await _uow.Customers.GetByEmailAsync(request.Email) != null)
            throw new BusinessRuleViolationException(ValidationMessages.EMAIL_ALREADY_EXISTS);

        if (!string.IsNullOrEmpty(request.PhoneNumber) && await _uow.Customers.GetByPhoneAsync(request.PhoneNumber) != null)
            throw new BusinessRuleViolationException(ValidationMessages.PHONE_ALREADY_EXISTS);

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.PhoneNumber,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            LoyaltyPoints = 0,
            MembershipLevel = "BRONZE",
            TotalSpent = 0.00m,
            VisitCount = 0
        };

        await _uow.Customers.AddAsync(customer);
        await _uow.SaveChangesAsync();

        return await GenerateTokenResponseAsync(customer.Id, customer.Email, customer.FullName, "CUSTOMER", "CUSTOMER");
    }

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken)
            ?? throw new BusinessRuleViolationException(ValidationMessages.ACCESS_TOKEN_INVALID);

        var jwtId = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
            ?? throw new BusinessRuleViolationException(ValidationMessages.ACCESS_TOKEN_INVALID);

        var storedToken = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken)
            ?? throw new BusinessRuleViolationException(ValidationMessages.REFRESH_TOKEN_NOT_FOUND);

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleViolationException(ValidationMessages.REFRESH_TOKEN_EXPIRED);

        if (storedToken.JwtId != jwtId)
            throw new BusinessRuleViolationException(ValidationMessages.REFRESH_TOKEN_MISMATCH);

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var email = principal.FindFirst(ClaimTypes.Email)!.Value;
        var fullName = principal.FindFirst(ClaimTypes.Name)!.Value;
        var role = principal.FindFirst(ClaimTypes.Role)!.Value;

        var (accessToken, newJwtId) = _jwtService.GenerateAccessToken(userId, email, fullName, role);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            JwtId = newJwtId,
            UserType = storedToken.UserType,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        storedToken.ReplacedByToken = newRefreshToken;

        await _uow.RefreshTokens.AddAsync(refreshTokenEntity);
        await _uow.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 3600,
            User = new UserInfoResponse
            {
                Id = userId,
                FullName = fullName,
                Email = email,
                Role = role
            }
        };
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        int userId;
        string userType;

        var user = await _uow.Users.GetByEmailAsync(request.Email);
        if (user != null)
        {
            userId = user.Id;
            userType = "USER";
        }
        else
        {
            var customer = await _uow.Customers.GetByEmailAsync(request.Email);
            if (customer == null)
            {
                return new ForgotPasswordResponse { Message = ValidationMessages.FORGOT_PASSWORD_MESSAGE };
            }
            userId = customer.Id;
            userType = "CUSTOMER";
        }

        await _uow.PasswordResetTokens.InvalidateAllByUserAsync(userId, userType);

        var resetToken = _jwtService.GeneratePasswordResetToken();
        var tokenEntity = new PasswordResetToken
        {
            Token = resetToken,
            Email = request.Email,
            UserType = userType,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        };

        await _uow.PasswordResetTokens.AddAsync(tokenEntity);
        await _uow.SaveChangesAsync();

        return new ForgotPasswordResponse
        {
            Message = ValidationMessages.FORGOT_PASSWORD_MESSAGE,
            ResetToken = resetToken
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new BusinessRuleViolationException(ValidationMessages.PASSWORD_CONFIRM_MISMATCH);

        var tokenEntity = await _uow.PasswordResetTokens.GetByTokenAsync(request.Token)
            ?? throw new BusinessRuleViolationException(ValidationMessages.RESET_TOKEN_INVALID);

        var newHash = _passwordHasher.HashPassword(request.NewPassword);

        if (tokenEntity.UserType == "USER")
        {
            var user = await _uow.Users.GetByIdAsync(tokenEntity.UserId)
                ?? throw new EntityNotFoundException("User", tokenEntity.UserId);
            user.PasswordHash = newHash;
        }
        else
        {
            var customer = await _uow.Customers.GetByIdAsync(tokenEntity.UserId)
                ?? throw new EntityNotFoundException("Customer", tokenEntity.UserId);
            customer.PasswordHash = newHash;
        }

        tokenEntity.IsUsed = true;

        await _uow.RefreshTokens.RevokeAllByUserAsync(tokenEntity.UserId, tokenEntity.UserType);
        await _uow.SaveChangesAsync();
    }

    public async Task<UserInfoResponse> GetCurrentUserAsync(int id, string role)
    {
        if (role == "CUSTOMER")
        {
            var customer = await _uow.Customers.GetByIdAsync(id);
            if (customer != null)
            {
                return new UserInfoResponse
                {
                    Id = customer.Id,
                    FullName = customer.FullName ?? "Customer",
                    Email = customer.Email ?? string.Empty,
                    Role = "CUSTOMER"
                };
            }
        }
        else
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user != null)
            {
                return new UserInfoResponse
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role
                };
            }
        }

        throw new EntityNotFoundException("User/Customer", id);
    }

    public async Task<GuestLoginResponse> LoginGuestAsync(GuestLoginRequest request)
    {
        var table = await _uow.Tables.GetByIdAsync(request.TableId)
            ?? throw new EntityNotFoundException("Table", request.TableId);

        var existingSession = await _uow.DiningSessions.GetActiveByTableIdAsync(request.TableId);
        DiningSession session;

        if (existingSession != null)
        {
            session = existingSession;
        }
        else
        {
            table.Status = "OCCUPIED";

            session = new DiningSession
            {
                TableId = table.Id,
                GuestName = request.GuestName,
                GuestPhone = request.GuestPhone,
                Status = "ACTIVE",
                StartedAt = DateTime.UtcNow
            };

            await _uow.DiningSessions.AddAsync(session);
            await _uow.SaveChangesAsync();
        }

        var guestName = request.GuestName ?? "Guest";
        var (accessToken, _) = _jwtService.GenerateAccessToken(session.Id, "", guestName, "GUEST");

        return new GuestLoginResponse
        {
            Token = accessToken,
            SessionId = session.Id,
            TableId = table.Id,
            TableNumber = table.TableNumber,
            Role = "GUEST"
        };
    }

    public async Task<LogoutResponse> LogoutAsync(int userId, string userType)
    {
        await _uow.RefreshTokens.RevokeAllByUserAsync(userId, userType);
        await _uow.SaveChangesAsync();

        return new LogoutResponse
        {
            Message = ValidationMessages.LOGOUT_SUCCESS
        };
    }

    private async Task<TokenResponse> GenerateTokenResponseAsync(int id, string email, string fullName, string role, string userType)
    {
        var (accessToken, jwtId) = _jwtService.GenerateAccessToken(id, email, fullName, role);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            JwtId = jwtId,
            UserType = userType,
            UserId = id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        await _uow.RefreshTokens.AddAsync(refreshTokenEntity);
        await _uow.SaveChangesAsync();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600,
            User = new UserInfoResponse
            {
                Id = id,
                FullName = fullName,
                Email = email,
                Role = role
            }
        };
    }
}
