using System.Collections.Generic;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Entity đại diện cho bàn ăn vật lý tại nhà hàng (dining_tables).
/// Mỗi bàn có số bàn duy nhất, sức chứa, mã QR để khách scan, và trạng thái realtime.
///
/// Trạng thái bàn (Status):
///   - AVAILABLE:   Bàn trống, sẵn sàng tiếp khách.
///   - OCCUPIED:    Đang có khách ngồi (có DiningSession ACTIVE).
///   - RESERVED:    Đã được đặt trước qua hệ thống booking.
///   - MAINTENANCE: Bàn đang bảo trì / hỏng, không phục vụ.
///
/// Quan hệ:
///   - 1:N với DiningSession  → lịch sử các phiên ăn tại bàn này.
///   - 1:N với TableReservation → các lịch đặt trước.
///   - 1:N với RobotDeliveryBatch → các đợt giao món bằng robot.
/// </summary>
public class Table : BaseEntity
{
    public int TableNumber { get; set; }
    public int Capacity { get; set; }
    public string? QrCode { get; set; }
    public TableStatus Status { get; set; } = TableStatus.AVAILABLE;

    // Navigation
    public List<DiningSession> DiningSessions { get; set; } = new();
    public List<TableReservation> Reservations { get; set; } = new();
    public List<RobotDeliveryBatch> DeliveryBatches { get; set; } = new();
}
