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
                throw new BusinessRuleViolationException(ValidationMessages.AUTH_INVALID_CREDENTIALS);

            return GenerateTokenResponse(user.Id, user.Email, user.FullName, user.Role);
        }

        var customer = await _uow.Customers.GetByEmailAsync(request.Email);
        if (customer != null)
        {
            if (customer.PasswordHash == null ||
                !_passwordHasher.VerifyPassword(request.Password, customer.PasswordHash))
                throw new BusinessRuleViolationException(ValidationMessages.AUTH_INVALID_CREDENTIALS);

            return GenerateTokenResponse(
                customer.Id,
                customer.Email ?? string.Empty,
                customer.FullName ?? "Customer",
                "CUSTOMER");
        }

        throw new BusinessRuleViolationException(ValidationMessages.AUTH_INVALID_CREDENTIALS);
    }

    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _uow.Users.ExistsAsync(request.Email) ||
            await _uow.Customers.GetByEmailAsync(request.Email) != null)
            throw new BusinessRuleViolationException(ValidationMessages.AUTH_EMAIL_ALREADY_EXISTS);

        if (await _uow.Customers.GetByPhoneAsync(request.PhoneNumber) != null)
            throw new BusinessRuleViolationException(ValidationMessages.AUTH_PHONE_ALREADY_EXISTS);

        var customer = new Customer
        {
            FullName         = request.FullName,
            Email            = request.Email,
            Phone            = request.PhoneNumber,
            PasswordHash     = _passwordHasher.HashPassword(request.Password),
            LoyaltyPoints    = 0,
            MembershipLevel  = "BRONZE",
            TotalSpent       = 0.00m,
            VisitCount       = 0
        };

        await _uow.Customers.AddAsync(customer);
        await _uow.SaveChangesAsync();

        return GenerateTokenResponse(customer.Id, customer.Email, customer.FullName, "CUSTOMER");
    }

    public async Task<UserInfoResponse> GetCurrentUserAsync(int id, string role)
    {
        if (role == "CUSTOMER")
        {
            var customer = await _uow.Customers.GetByIdAsync(id);
            if (customer != null)
                return new UserInfoResponse
                {
                    Id       = customer.Id,
                    FullName = customer.FullName ?? "Customer",
                    Email    = customer.Email ?? string.Empty,
                    Role     = "CUSTOMER"
                };
        }
        else
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user != null)
                return new UserInfoResponse
                {
                    Id       = user.Id,
                    FullName = user.FullName,
                    Email    = user.Email,
                    Role     = user.Role
                };
        }

        throw new EntityNotFoundException("User/Customer", id);
    }

    public async Task ChangePasswordAsync(int userId, string role, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            throw new BusinessRuleViolationException(ValidationMessages.AUTH_PASSWORD_CONFIRM_MISMATCH);

        if (role == "CUSTOMER")
        {
            var customer = await _uow.Customers.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("Customer", userId);

            if (customer.PasswordHash == null ||
                !_passwordHasher.VerifyPassword(request.CurrentPassword, customer.PasswordHash))
                throw new BusinessRuleViolationException(ValidationMessages.AUTH_CURRENT_PASSWORD_INCORRECT);

            customer.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _uow.Customers.UpdateAsync(customer);
        }
        else
        {
            var user = await _uow.Users.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                throw new BusinessRuleViolationException(ValidationMessages.AUTH_CURRENT_PASSWORD_INCORRECT);

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _uow.Users.UpdateAsync(user);
        }

        await _uow.SaveChangesAsync();
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        int id;
        string email, role;

        var user = await _uow.Users.GetByEmailAsync(request.Email);
        if (user != null)
        {
            id = user.Id; email = user.Email; role = user.Role;
        }
        else
        {
            var customer = await _uow.Customers.GetByEmailAsync(request.Email);
            if (customer == null)
                throw new BusinessRuleViolationException(ValidationMessages.AUTH_FORGOT_PASSWORD_GENERIC);

            id = customer.Id; email = customer.Email!; role = "CUSTOMER";
        }

        var resetToken = _jwtService.GeneratePasswordResetToken(id, email, role);

        // TODO: gửi resetToken qua email thay vì trả về trong response
        return new ForgotPasswordResponse
        {
            Message    = ValidationMessages.AUTH_FORGOT_PASSWORD_SENT,
            ResetToken = resetToken
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            throw new BusinessRuleViolationException(ValidationMessages.AUTH_PASSWORD_CONFIRM_MISMATCH);

        var principal = _jwtService.ValidatePasswordResetToken(request.ResetToken)
            ?? throw new BusinessRuleViolationException(ValidationMessages.AUTH_RESET_TOKEN_INVALID);

        var userId = int.Parse(
            principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

        if (role == "CUSTOMER")
        {
            var customer = await _uow.Customers.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("Customer", userId);
            customer.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _uow.Customers.UpdateAsync(customer);
        }
        else
        {
            var user = await _uow.Users.GetByIdAsync(userId)
                ?? throw new EntityNotFoundException("User", userId);
            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _uow.Users.UpdateAsync(user);
        }

        await _uow.SaveChangesAsync();
    }

    private TokenResponse GenerateTokenResponse(int id, string email, string fullName, string role)
    {
        var accessToken  = _jwtService.GenerateAccessToken(id, email, fullName, role);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new TokenResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn    = 3600,
            User = new UserInfoResponse
            {
                Id       = id,
                FullName = fullName,
                Email    = email,
                Role     = role
            }
        };
    }
}
