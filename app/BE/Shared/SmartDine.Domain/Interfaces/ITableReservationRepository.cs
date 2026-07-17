using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Repository contract cho entity TableReservation (lịch đặt bàn trước).
/// Kế thừa IRepository&lt;TableReservation&gt; (CRUD cơ bản) và bổ sung query cho nghiệp vụ booking.
/// </summary>
public interface ITableReservationRepository : IRepository<TableReservation>
{
    /// <summary>
    /// Lấy tất cả reservation của 1 bàn, sắp xếp theo thời gian đặt mới nhất.
    /// Dùng khi manager muốn xem lịch sử / lịch đặt sắp tới của bàn cụ thể.
    /// Include Customer + Table để hiển thị đầy đủ thông tin.
    /// </summary>
    Task<IReadOnlyList<TableReservation>> GetByTableIdAsync(int tableId);

    /// <summary>
    /// Lấy tất cả reservation của 1 khách hàng, sắp xếp theo thời gian đặt mới nhất.
    /// Dùng khi khách xem lại lịch sử đặt bàn của mình.
    /// Include Table để hiển thị số bàn.
    /// </summary>
    Task<IReadOnlyList<TableReservation>> GetByCustomerIdAsync(int customerId);

    /// <summary>
    /// Kiểm tra xung đột lịch đặt: tìm các reservation đang active (không phải CANCELLED/NO_SHOW)
    /// trong khoảng ±2 giờ so với thời gian đặt mới.
    ///
    /// Dùng khi tạo reservation mới → nếu trả về count > 0 thì bàn đã bị trùng lịch.
    /// Window ±2h đảm bảo mỗi lượt khách có đủ thời gian ăn mà không bị overlap.
    /// </summary>
    Task<IReadOnlyList<TableReservation>> GetActiveByTableAndTimeAsync(int tableId, DateTime reservationTime);
}
