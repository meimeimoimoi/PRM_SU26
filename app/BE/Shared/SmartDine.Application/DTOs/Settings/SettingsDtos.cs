namespace SmartDine.Application.DTOs.Settings;

// ─────────────────────────────────────────────────────────────
// Settings DTOs
// Cấu hình chung của nhà hàng (singleton row) — quản lý bởi Manager.
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Response trả về cấu hình nhà hàng hiện tại. Giờ mở/đóng cửa dạng chuỗi "HH:mm".
/// </summary>
public class SettingsResponse
{
    public int Id { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string OpeningTime { get; set; } = string.Empty;
    public string ClosingTime { get; set; } = string.Empty;
    public decimal TaxRate { get; set; }
    public decimal ServiceChargeRate { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request cập nhật cấu hình nhà hàng (partial update — chỉ field nào có giá trị mới bị thay đổi).
/// Giờ mở/đóng cửa gửi dạng chuỗi "HH:mm".
/// </summary>
public class UpdateSettingsRequest
{
    public string? RestaurantName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? ServiceChargeRate { get; set; }
}
