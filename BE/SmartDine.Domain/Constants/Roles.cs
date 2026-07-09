using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Constants;

/// <summary>
/// Hằng số role dùng trong [Authorize(Roles = ...)].
/// Compile-time const — an toàn khi đổi tên enum vì dùng nameof().
/// </summary>
public static class Roles
{
    // ── Single roles ──────────────────────────────────────────
    public const string Customer = nameof(UserRole.CUSTOMER);
    public const string Guest    = nameof(UserRole.GUEST);
    public const string Staff    = nameof(UserRole.STAFF);
    public const string Chef     = nameof(UserRole.CHEF);
    public const string Manager  = nameof(UserRole.MANAGER);

    // ── Role groups (comma-separated, dùng cho Authorize attribute) ──
    /// <summary>CUSTOMER, GUEST — khách ngồi bàn.</summary>
    public const string AllDiners = Customer + "," + Guest;

    /// <summary>CUSTOMER, GUEST, STAFF — ai có thể đặt món.</summary>
    public const string AllDinersAndStaff = Customer + "," + Guest + "," + Staff;

    /// <summary>STAFF, CHEF, MANAGER — nhân viên bếp/phục vụ.</summary>
    public const string KitchenStaff = Staff + "," + Chef + "," + Manager;

    /// <summary>MANAGER, CHEF — quản lý hoặc đầu bếp.</summary>
    public const string ManagerAndChef = Manager + "," + Chef;

    /// <summary>STAFF, MANAGER — quản lý bàn / reservation.</summary>
    public const string StaffAndManager = Staff + "," + Manager;

    /// <summary>CUSTOMER, STAFF, MANAGER — đặt bàn trước.</summary>
    public const string CustomerAndManagement = Customer + "," + Staff + "," + Manager;

    /// <summary>CUSTOMER, GUEST, STAFF, MANAGER — tất cả trừ CHEF.</summary>
    public const string AllExceptChef = Customer + "," + Guest + "," + Staff + "," + Manager;
}
