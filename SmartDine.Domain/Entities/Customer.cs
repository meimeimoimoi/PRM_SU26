namespace SmartDine.Domain.Entities;

/// <summary>
/// Hồ sơ khách hàng — mở rộng từ User, chứa thông tin loyalty, preferences.
/// </summary>
public class Customer : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? DietaryPreferences { get; set; }
    public string? AvatarUrl { get; set; }

    // Navigation
    public LoyaltyAccount? LoyaltyAccount { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public List<DiningSession> DiningSessions { get; set; } = new();
    public List<CustomerActivity> Activities { get; set; } = new();
}
