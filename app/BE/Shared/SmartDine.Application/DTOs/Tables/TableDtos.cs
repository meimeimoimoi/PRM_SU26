using SmartDine.Domain.Enums;

namespace SmartDine.Application.DTOs.Tables;

// ─────────────────────────────────────────────────────────────
// Table DTOs
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Response trả về thông tin 1 bàn ăn.
/// Dùng cho API GET /api/v1/tables (danh sách) và các response liên quan đến bàn.
/// </summary>
public class TableResponse
{
    public int Id { get; set; }
    public int TableNumber { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = nameof(TableStatus.AVAILABLE);
    public string? QrCode { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
}

/// <summary>
/// Request tạo bàn mới (Manager only).
/// TableNumber phải là duy nhất trong hệ thống.
/// Capacity mặc định 4 nếu client không gửi.
/// LocationId tùy chọn — không truyền thì bàn chưa gán khu vực.
/// </summary>
public class CreateTableRequest
{
    public int TableNumber { get; set; }
    public int Capacity { get; set; } = 4;
    public int? LocationId { get; set; }
}

/// <summary>
/// Request body cho API PATCH /api/v1/tables/{id}/status.
/// Staff/Manager gửi trạng thái mới: AVAILABLE, OCCUPIED, RESERVED, MAINTENANCE.
/// </summary>
public class UpdateTableStatusRequest
{
    public string Status { get; set; } = nameof(TableStatus.AVAILABLE);
}

/// <summary>
/// Request cập nhật thông tin cơ bản của bàn (Manager only) — partial update.
/// Không cho sửa TableNumber vì mã QR đã in mã hóa theo số bàn cũ, đổi số sẽ làm QR sai.
/// Muốn đổi số bàn thì xóa bàn và tạo bàn mới để có QR đúng.
/// </summary>
public class UpdateTableRequest
{
    public int? Capacity { get; set; }
    public int? LocationId { get; set; }
}

// ─────────────────────────────────────────────────────────────
// Location DTOs
// ─────────────────────────────────────────────────────────────

public class LocationResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Request tạo khu vực/vị trí bàn mới (Manager only).
/// </summary>
public class CreateLocationRequest
{
    public string Name { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────
// Scan QR DTOs
// Khách quét mã QR trên bàn → hệ thống tạo/tham gia phiên ăn.
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Request body cho API POST /api/v1/tables/{id}/scan.
/// CustomerId nullable: nếu null → khách vãng lai (guest), nếu có → khách thành viên.
/// GuestSessionId: set bởi controller từ JWT sub claim khi role=GUEST.
/// </summary>
public class ScanTableRequest
{
    public int? CustomerId { get; set; }
    public string? GuestSessionId { get; set; }
}

/// <summary>
/// Response sau khi quét QR bàn thành công.
/// - IsNewSession = true  → bàn trống, hệ thống vừa tạo phiên ăn mới.
/// - IsNewSession = false → bàn đã có người, khách tham gia nhóm gọi món hiện tại.
/// </summary>
public class ScanTableResponse
{
    public int SessionId { get; set; }
    public int TableId { get; set; }
    public int TableNumber { get; set; }
    public string Status { get; set; } = nameof(DiningSessionStatus.ACTIVE);
    public bool IsNewSession { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ─────────────────────────────────────────────────────────────
// Update Status Response
// ─────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────
// Reservation DTOs
// Đặt bàn trước (booking) và cập nhật trạng thái đặt bàn.
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Request body cho API POST /api/v1/tables/reservations.
///
/// Hai trường hợp:
///   1. Khách thành viên tự đặt → gửi CustomerId, có thể bỏ GuestName/GuestPhone.
///   2. Nhân viên nhập hộ khách vãng lai → gửi GuestName + GuestPhone, CustomerId = null.
///
/// Service sẽ validate:
///   - PartySize > 0 và không vượt sức chứa bàn.
///   - ReservationTime phải ở tương lai.
///   - Không trùng lịch với reservation khác trong ±2 giờ.
/// </summary>
public class CreateReservationRequest
{
    public int? CustomerId { get; set; }
    public int TableId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public int PartySize { get; set; }
    public DateTime ReservationTime { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request body cho API PATCH /api/v1/tables/reservations/{id}/status.
/// Trạng thái hợp lệ: PENDING, CONFIRMED, CHECKED_IN, CANCELLED, NO_SHOW.
/// </summary>
public class UpdateReservationStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Response sau khi tạo reservation thành công.
/// Trả về đầy đủ thông tin đặt bàn bao gồm TableNumber để client hiển thị.
/// </summary>
public class ReservationResponse
{
    public int ReservationId { get; set; }
    public int TableId { get; set; }
    public int TableNumber { get; set; }
    public int? CustomerId { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public int PartySize { get; set; }
    public DateTime ReservationTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// Response sau khi cập nhật trạng thái reservation.
/// TableStatus chỉ có giá trị khi status = CHECKED_IN (bàn chuyển OCCUPIED),
/// các trạng thái khác TableStatus = null (bàn không bị ảnh hưởng).
/// </summary>
public class UpdateReservationStatusResponse
{
    public int ReservationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TableStatus { get; set; }
}
