namespace SmartDine.Domain.Entities;

/// <summary>
/// Cấu hình chung của nhà hàng (restaurant_settings) — chỉ có 1 bản ghi duy nhất (singleton row).
/// Quản lý bởi Manager qua SettingsController: thông tin nhà hàng, giờ mở/đóng cửa, thuế, phí dịch vụ.
/// </summary>
public class RestaurantSettings : BaseEntity
{
    public string RestaurantName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public TimeSpan OpeningTime { get; set; }
    public TimeSpan ClosingTime { get; set; }

    /// <summary>Thuế suất (%), VD: 8.00 = 8%.</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Phí dịch vụ (%), VD: 5.00 = 5%.</summary>
    public decimal ServiceChargeRate { get; set; }
}
