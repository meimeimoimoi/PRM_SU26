using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SmartDine.Application.DTOs.Auth;
using SmartDine.Application.DTOs.Common;
using SmartDine.Application.DTOs.Menu;
using SmartDine.Application.DTOs.Orders;
using SmartDine.Application.DTOs.Tables;
using SmartDine.Domain.Entities;
using SmartDine.Infrastructure.Persistence;

namespace SmartDine.Tests;

public class IntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public IntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Auth_Register_And_Login_Should_Return_RSA_Signed_Tokens()
    {
        var client = _factory.CreateClient();
        var email = $"customer_{Guid.NewGuid()}@example.com";

        var registerRequest = new RegisterRequest
        {
            FullName = "Test Customer",
            Email = email,
            Password = "Password123!",
            PhoneNumber = $"09{Guid.NewGuid().ToString()[..8]}"
        };

        // Register
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();
        Assert.NotNull(registerResult);
        Assert.True(registerResult.Success);
        Assert.NotNull(registerResult.Data!.AccessToken);
        Assert.NotNull(registerResult.Data!.RefreshToken);

        // Login
        var loginRequest = new LoginRequest { Email = email, Password = "Password123!" };
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();
        Assert.NotNull(loginResult);
        Assert.True(loginResult.Success);
        Assert.NotNull(loginResult.Data!.AccessToken);
        Assert.NotNull(loginResult.Data!.RefreshToken);

        // Verify token works for authenticated endpoint
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Data.AccessToken);
        var meResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var meResult = await meResponse.Content.ReadFromJsonAsync<ApiResponse<UserInfoResponse>>();
        Assert.NotNull(meResult);
        Assert.True(meResult.Success);
        Assert.Equal(email, meResult.Data!.Email);
        Assert.Equal("CUSTOMER", meResult.Data.Role);
    }

    [Fact]
    public async Task RefreshToken_Should_Return_New_TokenPair()
    {
        var client = _factory.CreateClient();
        var email = $"refresh_{Guid.NewGuid()}@example.com";

        // Register to get initial tokens
        var registerRequest = new RegisterRequest
        {
            FullName = "Refresh Test",
            Email = email,
            Password = "Password123!",
            PhoneNumber = $"09{Guid.NewGuid().ToString()[..8]}"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();
        Assert.NotNull(registerResult?.Data);

        // Refresh token
        var refreshRequest = new RefreshTokenRequest
        {
            AccessToken = registerResult.Data.AccessToken,
            RefreshToken = registerResult.Data.RefreshToken
        };
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh-token", refreshRequest);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();
        Assert.NotNull(refreshResult);
        Assert.True(refreshResult.Success);
        Assert.NotNull(refreshResult.Data!.AccessToken);
        Assert.NotNull(refreshResult.Data!.RefreshToken);

        // New token should be different
        Assert.NotEqual(registerResult.Data.AccessToken, refreshResult.Data.AccessToken);
        Assert.NotEqual(registerResult.Data.RefreshToken, refreshResult.Data.RefreshToken);

        // New access token should work
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshResult.Data.AccessToken);
        var meResponse = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_With_Used_Token_Should_Fail()
    {
        var client = _factory.CreateClient();
        var email = $"used_refresh_{Guid.NewGuid()}@example.com";

        var registerRequest = new RegisterRequest
        {
            FullName = "Used Refresh Test",
            Email = email,
            Password = "Password123!",
            PhoneNumber = $"09{Guid.NewGuid().ToString()[..8]}"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();

        var refreshRequest = new RefreshTokenRequest
        {
            AccessToken = registerResult!.Data!.AccessToken,
            RefreshToken = registerResult.Data.RefreshToken
        };

        // First refresh — should succeed
        var firstRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh-token", refreshRequest);
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        // Second refresh with same token — should fail (token revoked)
        var secondRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh-token", refreshRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, secondRefresh.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_And_ResetPassword_Should_Work()
    {
        var client = _factory.CreateClient();
        var email = $"reset_{Guid.NewGuid()}@example.com";
        var originalPassword = "Password123!";
        var newPassword = "NewPassword456!";

        // Register
        var registerRequest = new RegisterRequest
        {
            FullName = "Reset Test",
            Email = email,
            Password = originalPassword,
            PhoneNumber = $"09{Guid.NewGuid().ToString()[..8]}"
        };
        await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Forgot password
        var forgotRequest = new ForgotPasswordRequest { Email = email };
        var forgotResponse = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", forgotRequest);
        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);
        var forgotResult = await forgotResponse.Content.ReadFromJsonAsync<ApiResponse<ForgotPasswordResponse>>();
        Assert.NotNull(forgotResult?.Data?.ResetToken);

        // Reset password
        var resetRequest = new ResetPasswordRequest
        {
            Token = forgotResult.Data.ResetToken,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };
        var resetResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);

        // Login with old password should fail
        var oldLoginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Email = email, Password = originalPassword });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, oldLoginResponse.StatusCode);

        // Login with new password should succeed
        var newLoginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Email = email, Password = newPassword });
        Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);
        var newLoginResult = await newLoginResponse.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();
        Assert.True(newLoginResult!.Success);
    }

    [Fact]
    public async Task ResetPassword_With_Invalid_Token_Should_Fail()
    {
        var client = _factory.CreateClient();

        var resetRequest = new ResetPasswordRequest
        {
            Token = "invalid_token_here",
            NewPassword = "NewPassword456!",
            ConfirmPassword = "NewPassword456!"
        };
        var resetResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resetResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_With_Mismatched_Passwords_Should_Fail()
    {
        var client = _factory.CreateClient();
        var email = $"mismatch_{Guid.NewGuid()}@example.com";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            FullName = "Mismatch Test",
            Email = email,
            Password = "Password123!",
            PhoneNumber = $"09{Guid.NewGuid().ToString()[..8]}"
        });

        var forgotResponse = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { Email = email });
        var forgotResult = await forgotResponse.Content.ReadFromJsonAsync<ApiResponse<ForgotPasswordResponse>>();

        var resetRequest = new ResetPasswordRequest
        {
            Token = forgotResult!.Data!.ResetToken!,
            NewPassword = "NewPassword456!",
            ConfirmPassword = "DifferentPassword789!"
        };
        var resetResponse = await client.PostAsJsonAsync("/api/v1/auth/reset-password", resetRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, resetResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_With_Nonexistent_Email_Should_Still_Return_OK()
    {
        var client = _factory.CreateClient();

        var forgotResponse = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new ForgotPasswordRequest { Email = "nonexistent@example.com" });

        // Should not reveal whether email exists
        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);
        var result = await forgotResponse.Content.ReadFromJsonAsync<ApiResponse<ForgotPasswordResponse>>();
        Assert.NotNull(result?.Data?.Message);
        Assert.Null(result.Data.ResetToken);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Should_Return_422()
    {
        var client = _factory.CreateClient();

        var loginRequest = new LoginRequest { Email = "admin@smartdine.com", Password = "WrongPassword!" };
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Tables_And_Orders_Flow_Should_Work_For_Authorized_Users()
    {
        var client = _factory.CreateClient();

        // Login as Manager
        var adminLoginRequest = new LoginRequest { Email = "admin@smartdine.com", Password = "Password123!" };
        var adminLoginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", adminLoginRequest);
        var adminLoginResult = await adminLoginResponse.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();

        Assert.NotNull(adminLoginResult);
        Assert.True(adminLoginResult.Success);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLoginResult.Data!.AccessToken);

        // Fetch tables
        var tablesResponse = await client.GetAsync("/api/v1/tables");
        Assert.Equal(HttpStatusCode.OK, tablesResponse.StatusCode);
        var tablesResult = await tablesResponse.Content.ReadFromJsonAsync<ApiResponse<List<TableResponse>>>();
        Assert.NotNull(tablesResult);
        Assert.True(tablesResult.Success);
        Assert.NotEmpty(tablesResult.Data!);

        // Fetch menu items
        var menuResponse = await client.GetAsync("/api/v1/menu-items");
        Assert.Equal(HttpStatusCode.OK, menuResponse.StatusCode);
        var menuResult = await menuResponse.Content.ReadFromJsonAsync<ApiResponse<List<MenuItemResponse>>>();
        Assert.NotNull(menuResult);
        Assert.True(menuResult.Success);
        Assert.NotEmpty(menuResult.Data!);

        // Register customer and place order
        var customerEmail = $"order_customer_{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            FullName = "Dining Customer",
            Email = customerEmail,
            Password = "Password123!",
            PhoneNumber = $"09{Guid.NewGuid().ToString()[..8]}"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<TokenResponse>>();

        Assert.NotNull(registerResult);
        Assert.True(registerResult.Success);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerResult.Data!.AccessToken);

        var targetMenuItemIds = new List<int> { menuResult.Data![0].Id, menuResult.Data![1].Id };
        var placeOrderRequest = new PlaceOrderRequest
        {
            TableId = tablesResult.Data![0].Id,
            Items = targetMenuItemIds.Select(id => new OrderDetailRequest { MenuItemId = id, Quantity = 1 }).ToList()
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartDineDbContext>();
            var customer = db.Customers.First(c => c.Email == customerEmail);
            var table = db.Tables.First(t => t.Id == tablesResult.Data![0].Id);

            var session = new DiningSession
            {
                CustomerId = customer.Id,
                TableId = table.Id,
                GuestName = customer.FullName,
                GuestPhone = customer.Phone,
                Status = "ACTIVE",
                TotalSpent = 0.00m
            };
            db.DiningSessions.Add(session);
            db.SaveChanges();

            placeOrderRequest.DiningSessionId = session.Id;
        }

        var orderResponse = await client.PostAsJsonAsync("/api/v1/orders", placeOrderRequest);
        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);
        var orderResult = await orderResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>();
        Assert.NotNull(orderResult);
        Assert.True(orderResult.Success);
        Assert.Equal("PENDING", orderResult.Data!.Status);
    }
}
