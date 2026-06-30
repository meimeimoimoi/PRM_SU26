using System;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Entity đại diện cho lịch đặt bàn trước (table_reservations).
/// Khách thành viên hoặc nhân viên nhập hộ đều có thể tạo reservation.
///
/// Vòng đời trạng thái (Status):
///   PENDING → CONFIRMED → CHECKED_IN   (happy path: khách đặt → xác nhận → đến nhà hàng)
///   PENDING → CANCELLED                 (khách hủy trước giờ hẹn)
///   CONFIRMED → NO_SHOW                 (khách xác nhận nhưng không đến)
///   CONFIRMED → CANCELLED               (khách hủy sau khi đã xác nhận)
///
/// Khi CHECKED_IN:
///   - Bàn tự động chuyển OCCUPIED.
///   - Hệ thống tạo DiningSession mới để khách bắt đầu gọi món.
///
/// Quan hệ:
///   - N:1 với Customer (nullable — guest không cần tài khoản).
///   - N:1 với Table.
/// </summary>
public class TableReservation : BaseEntity
{
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int TableId { get; set; }
    public Table Table { get; set; } = null!;

    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public int PartySize { get; set; }
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime ReservationTime { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.PENDING;
    public string? Notes { get; set; }
}
