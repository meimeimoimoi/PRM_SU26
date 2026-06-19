namespace SmartDine.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserType { get; set; } = "CUSTOMER"; // USER, CUSTOMER
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
}
