using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Tài khoản người dùng — Staff, Manager, Chef đều dùng entity này.
/// Customer có entity riêng liên kết qua UserId.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.CUSTOMER;
    public bool IsActive { get; set; } = true;
    public string? AvatarUrl { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
}
