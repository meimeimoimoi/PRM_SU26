using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Application.Services;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;
using SmartDine.Infrastructure.Security;

namespace SmartDine.Tests.Unit;

// ─── Fakes ────────────────────────────────────────────────────────────────────

public class FakePasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => $"hashed:{password}";
    public bool VerifyPassword(string plain, string hashed) => hashed == $"hashed:{plain}";
}

public class FakeUnitOfWork : IUnitOfWork
{
    public FakeUserRepository Users { get; } = new();
    public FakeCustomerRepository Customers { get; } = new();

    IUserRepository IUnitOfWork.Users => Users;
    ICustomerRepository IUnitOfWork.Customers => Customers;
    IOrderRepository IUnitOfWork.Orders => throw new NotImplementedException();
    IMenuItemRepository IUnitOfWork.MenuItems => throw new NotImplementedException();
    ITableRepository IUnitOfWork.Tables => throw new NotImplementedException();
    IDiningSessionRepository IUnitOfWork.DiningSessions => throw new NotImplementedException();
    IPaymentRepository IUnitOfWork.Payments => throw new NotImplementedException();
    IReviewRepository IUnitOfWork.Reviews => throw new NotImplementedException();

    public Task<int> SaveChangesAsync(CancellationToken ct = default) { SaveCount++; return Task.FromResult(1); }
    public int SaveCount { get; private set; }
    public void Dispose() { }
}

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _store = [];

    public void Seed(User user) => _store.Add(user);

    public Task<User?> GetByIdAsync(int id) =>
        Task.FromResult(_store.FirstOrDefault(u => u.Id == id));
    public Task<User?> GetByEmailAsync(string email) =>
        Task.FromResult(_store.FirstOrDefault(u => u.Email == email));
    public Task<bool> ExistsAsync(string email) =>
        Task.FromResult(_store.Any(u => u.Email == email));
    public Task UpdateAsync(User entity)
    {
        var idx = _store.FindIndex(u => u.Id == entity.Id);
        if (idx >= 0) _store[idx] = entity;
        return Task.CompletedTask;
    }

    public Task<User> AddAsync(User entity) { _store.Add(entity); return Task.FromResult(entity); }
    public Task<IReadOnlyList<User>> GetAllAsync() => Task.FromResult<IReadOnlyList<User>>(_store);
    public Task<IReadOnlyList<User>> GetPagedAsync(int p, int s) => Task.FromResult<IReadOnlyList<User>>(_store);
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task<int> CountAsync() => Task.FromResult(_store.Count);
}

public class FakeCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _store = [];

    public void Seed(Customer c) => _store.Add(c);

    public Task<Customer?> GetByIdAsync(int id) =>
        Task.FromResult(_store.FirstOrDefault(c => c.Id == id));
    public Task<Customer?> GetByEmailAsync(string email) =>
        Task.FromResult(_store.FirstOrDefault(c => c.Email == email));
    public Task<Customer?> GetByPhoneAsync(string phone) =>
        Task.FromResult(_store.FirstOrDefault(c => c.Phone == phone));
    public Task UpdateAsync(Customer entity)
    {
        var idx = _store.FindIndex(c => c.Id == entity.Id);
        if (idx >= 0) _store[idx] = entity;
        return Task.CompletedTask;
    }

    public Task<Customer> AddAsync(Customer entity) { _store.Add(entity); return Task.FromResult(entity); }
    public Task<IReadOnlyList<Customer>> GetAllAsync() => Task.FromResult<IReadOnlyList<Customer>>(_store);
    public Task<IReadOnlyList<Customer>> GetPagedAsync(int p, int s) => Task.FromResult<IReadOnlyList<Customer>>(_store);
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task<int> CountAsync() => Task.FromResult(_store.Count);
}

// ─── Tests ────────────────────────────────────────────────────────────────────

public class AuthServicePasswordTests
{
    private readonly FakeUnitOfWork _uow;
    private readonly FakePasswordHasher _hasher;
    private readonly JwtTokenService _jwtService;
    private readonly AuthService _sut;

    public AuthServicePasswordTests()
    {
        _uow    = new FakeUnitOfWork();
        _hasher = new FakePasswordHasher();

        var keyProvider = new FakeRsaKeyProvider();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"]                   = "TestIssuer",
                ["Jwt:Audience"]                 = "TestAudience",
                ["Jwt:AccessTokenExpiryMinutes"] = "60",
            })
            .Build();

        _jwtService = new JwtTokenService(config, keyProvider);
        _sut = new AuthService(_uow, _jwtService, _hasher);
    }

    // ── ChangePassword — User (staff) ──────────────────────────────────────

    [Fact]
    public async Task ChangePassword_User_SuccessWithCorrectCurrentPassword()
    {
        var user = new User { Id = 1, Email = "staff@test.com", PasswordHash = "hashed:OldPass1!", Role = "STAFF", IsActive = true };
        _uow.Users.Seed(user);

        await _sut.ChangePasswordAsync(1, "STAFF", new ChangePasswordRequest
        {
            CurrentPassword    = "OldPass1!",
            NewPassword        = "NewPass2@",
            ConfirmNewPassword = "NewPass2@"
        });

        var updated = await _uow.Users.GetByIdAsync(1);
        Assert.Equal("hashed:NewPass2@", updated!.PasswordHash);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task ChangePassword_User_ThrowsWhenCurrentPasswordWrong()
    {
        _uow.Users.Seed(new User { Id = 2, Email = "u@test.com", PasswordHash = "hashed:Correct!", Role = "STAFF" });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _sut.ChangePasswordAsync(2, "STAFF", new ChangePasswordRequest
            {
                CurrentPassword    = "Wrong!",
                NewPassword        = "New1!",
                ConfirmNewPassword = "New1!"
            }));

        Assert.Contains("Current password", ex.Message);
    }

    [Fact]
    public async Task ChangePassword_User_ThrowsWhenConfirmPasswordMismatch()
    {
        _uow.Users.Seed(new User { Id = 3, Email = "u3@test.com", PasswordHash = "hashed:Pass!", Role = "STAFF" });

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _sut.ChangePasswordAsync(3, "STAFF", new ChangePasswordRequest
            {
                CurrentPassword    = "Pass!",
                NewPassword        = "NewA",
                ConfirmNewPassword = "NewB"  // does not match
            }));

        Assert.Contains("does not match", ex.Message);
    }

    // ── ChangePassword — Customer ──────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_Customer_SuccessWithCorrectCurrentPassword()
    {
        var customer = new Customer { Id = 10, Email = "cus@test.com", PasswordHash = "hashed:OldCus1!" };
        _uow.Customers.Seed(customer);

        await _sut.ChangePasswordAsync(10, "CUSTOMER", new ChangePasswordRequest
        {
            CurrentPassword    = "OldCus1!",
            NewPassword        = "NewCus2@",
            ConfirmNewPassword = "NewCus2@"
        });

        var updated = await _uow.Customers.GetByIdAsync(10);
        Assert.Equal("hashed:NewCus2@", updated!.PasswordHash);
    }

    [Fact]
    public async Task ChangePassword_Customer_ThrowsWhenCurrentPasswordWrong()
    {
        _uow.Customers.Seed(new Customer { Id = 11, Email = "c11@test.com", PasswordHash = "hashed:Correct!" });

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _sut.ChangePasswordAsync(11, "CUSTOMER", new ChangePasswordRequest
            {
                CurrentPassword    = "Wrong!",
                NewPassword        = "New!",
                ConfirmNewPassword = "New!"
            }));
    }

    [Fact]
    public async Task ChangePassword_ThrowsWhenUserNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _sut.ChangePasswordAsync(999, "STAFF", new ChangePasswordRequest
            {
                CurrentPassword    = "Pass",
                NewPassword        = "New",
                ConfirmNewPassword = "New"
            }));
    }

    // ── ForgotPassword ─────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_ReturnsResetTokenForExistingUser()
    {
        _uow.Users.Seed(new User { Id = 5, Email = "manager@test.com", PasswordHash = "hashed:P!", Role = "MANAGER" });

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "manager@test.com" });

        Assert.NotNull(result.ResetToken);
        Assert.NotEmpty(result.ResetToken);
    }

    [Fact]
    public async Task ForgotPassword_ReturnsResetTokenForExistingCustomer()
    {
        _uow.Customers.Seed(new Customer { Id = 20, Email = "cus20@test.com" });

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "cus20@test.com" });

        Assert.NotNull(result.ResetToken);
        Assert.NotEmpty(result.ResetToken);
    }

    [Fact]
    public async Task ForgotPassword_ThrowsForUnknownEmail()
    {
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nobody@test.com" }));
    }

    [Fact]
    public async Task ForgotPassword_ResetTokenContainsPurposeClaim()
    {
        _uow.Users.Seed(new User { Id = 6, Email = "chef@test.com", PasswordHash = "hashed:P!", Role = "CHEF" });

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "chef@test.com" });

        var principal = _jwtService.ValidatePasswordResetToken(result.ResetToken);
        Assert.NotNull(principal);
        Assert.Equal("password-reset", principal!.FindFirst("purpose")?.Value);
    }

    // ── ResetPassword ──────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_UpdatesPasswordForUser()
    {
        var user = new User { Id = 7, Email = "reset@test.com", PasswordHash = "hashed:Old!", Role = "STAFF" };
        _uow.Users.Seed(user);

        var token = _jwtService.GeneratePasswordResetToken(7, "reset@test.com", "STAFF");

        await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            ResetToken         = token,
            NewPassword        = "BrandNew1!",
            ConfirmNewPassword = "BrandNew1!"
        });

        var updated = await _uow.Users.GetByIdAsync(7);
        Assert.Equal("hashed:BrandNew1!", updated!.PasswordHash);
    }

    [Fact]
    public async Task ResetPassword_UpdatesPasswordForCustomer()
    {
        var customer = new Customer { Id = 30, Email = "cusr@test.com", PasswordHash = "hashed:Old!" };
        _uow.Customers.Seed(customer);

        var token = _jwtService.GeneratePasswordResetToken(30, "cusr@test.com", "CUSTOMER");

        await _sut.ResetPasswordAsync(new ResetPasswordRequest
        {
            ResetToken         = token,
            NewPassword        = "NewCus!1",
            ConfirmNewPassword = "NewCus!1"
        });

        var updated = await _uow.Customers.GetByIdAsync(30);
        Assert.Equal("hashed:NewCus!1", updated!.PasswordHash);
    }

    [Fact]
    public async Task ResetPassword_ThrowsWhenTokenInvalid()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _sut.ResetPasswordAsync(new ResetPasswordRequest
            {
                ResetToken         = "invalid.token.here",
                NewPassword        = "New!",
                ConfirmNewPassword = "New!"
            }));

        Assert.Contains("invalid or has expired", ex.Message);
    }

    [Fact]
    public async Task ResetPassword_ThrowsWhenConfirmPasswordMismatch()
    {
        var user = new User { Id = 8, Email = "mm@test.com", PasswordHash = "hashed:P!", Role = "STAFF" };
        _uow.Users.Seed(user);
        var token = _jwtService.GeneratePasswordResetToken(8, "mm@test.com", "STAFF");

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _sut.ResetPasswordAsync(new ResetPasswordRequest
            {
                ResetToken         = token,
                NewPassword        = "NewA!",
                ConfirmNewPassword = "NewB!"
            }));

        Assert.Contains("does not match", ex.Message);
    }

    [Fact]
    public async Task ResetPassword_ThrowsWhenAccessTokenUsedInsteadOfResetToken()
    {
        // Access token has no purpose=password-reset claim and must be rejected
        var accessToken = _jwtService.GenerateAccessToken(1, "x@test.com", "X", "STAFF");

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _sut.ResetPasswordAsync(new ResetPasswordRequest
            {
                ResetToken         = accessToken,
                NewPassword        = "New!",
                ConfirmNewPassword = "New!"
            }));

        Assert.Contains("invalid or has expired", ex.Message);
    }
}
