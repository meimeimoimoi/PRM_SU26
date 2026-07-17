namespace SmartDine.Application.DTOs.Staff;

// ─────────────────────────────────────────────────────────────
// Staff DTOs
// Quản lý tài khoản nhân viên nội bộ (STAFF / CHEF / MANAGER).
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Response trả về thông tin 1 tài khoản nhân viên.
/// </summary>
public class StaffResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request tạo tài khoản nhân viên mới (Manager only).
/// Manager đặt mật khẩu trực tiếp cho nhân viên — không có luồng mời qua email.
/// Role chỉ chấp nhận STAFF, CHEF, MANAGER.
/// </summary>
public class CreateStaffRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Request cập nhật thông tin nhân viên (partial update — chỉ field nào có giá trị mới bị thay đổi).
/// </summary>
public class UpdateStaffRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
