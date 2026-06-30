using SmartDine.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Repository contract cho entity Table (bàn ăn).
/// Kế thừa IRepository&lt;Table&gt; (CRUD cơ bản) và bổ sung các query đặc thù cho nghiệp vụ quản lý bàn.
/// </summary>
public interface ITableRepository : IRepository<Table>
{
    /// <summary>
    /// Lấy danh sách bàn theo trạng thái (VD: "AVAILABLE", "OCCUPIED").
    /// Dùng khi nhân viên cần xem nhanh bàn trống hoặc bàn đang có khách.
    /// </summary>
    Task<IReadOnlyList<Table>> GetByStatusAsync(string status);

    /// <summary>
    /// Tìm bàn theo số bàn (table_number).
    /// Dùng khi cần tra cứu bàn theo số bàn vật lý tại nhà hàng (VD: bàn số 12).
    /// </summary>
    Task<Table?> GetByTableNumberAsync(int tableNumber);

    /// <summary>
    /// Lấy danh sách bàn với bộ lọc tùy chọn theo trạng thái và sức chứa tối thiểu.
    /// Phục vụ API GET /api/v1/tables?status=AVAILABLE&amp;capacity=4.
    ///
    /// Luồng: Controller nhận query params → Service validate status → gọi method này.
    /// - status = null → không lọc trạng thái (trả tất cả).
    /// - capacity = null → không lọc sức chứa.
    /// - Cả hai đều có → AND condition (VD: bàn trống có ít nhất 4 chỗ).
    /// Kết quả sắp xếp theo TableNumber tăng dần.
    /// </summary>
    Task<IReadOnlyList<Table>> GetFilteredAsync(string? status, int? capacity);
}
