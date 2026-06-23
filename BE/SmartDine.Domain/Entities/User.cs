namespace SmartDine.Domain.Entities;

/// <summary>
/// Entity đại diện cho nhân viên hệ thống (users).
/// Đây là tài khoản nội bộ nhà hàng, KHÔNG phải khách hàng.
///
/// Phân loại theo Role:
///   - STAFF:   Nhân viên phục vụ — dọn bàn, cập nhật trạng thái bàn/order.
///   - CHEF:    Đầu bếp — nhận order từ bếp, cập nhật trạng thái món (COOKING → COMPLETED).
///   - MANAGER: Quản lý — toàn quyền CRUD menu, bàn, xem báo cáo, quản lý nhân viên.
///
/// Xác thực: Email + PasswordHash (BCrypt).
/// IsActive = false → tài khoản bị vô hiệu hóa, không thể login.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "STAFF";
    public bool IsActive { get; set; } = true;
}
