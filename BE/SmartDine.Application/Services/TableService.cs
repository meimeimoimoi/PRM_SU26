using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.Tables;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý nghiệp vụ quản lý bàn ăn và đặt bàn trước.
///
/// Chịu trách nhiệm:
///   - Truy vấn danh sách bàn có filter (API 1).
///   - Xử lý quét QR tại bàn: tạo hoặc tham gia phiên ăn (API 2).
///   - Cập nhật trạng thái bàn kèm side-effect đóng session (API 3).
///   - Tạo lịch đặt bàn trước với kiểm tra xung đột (API 4).
///   - Cập nhật trạng thái reservation kèm side-effect tạo session khi check-in (API 5).
///
/// Dependency: IUnitOfWork (truy cập Tables, DiningSessions, Customers, TableReservations).
/// </summary>
public class TableService
{
    private readonly IUnitOfWork _uow;

    public TableService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // ═══════════════════════════════════════════════════════════════
    // API 1: GET /api/v1/tables?status=AVAILABLE&capacity=4
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Lấy danh sách bàn ăn, hỗ trợ filter theo trạng thái và sức chứa tối thiểu.
    ///
    /// Luồng xử lý:
    ///   1. Controller nhận query params ?status=&amp;capacity= từ request.
    ///   2. Validate: nếu có status thì phải thuộc enum TableStatus, không thì throw 422.
    ///   3. Gọi repo GetFilteredAsync() → dynamic query (WHERE + ORDER BY table_number).
    ///   4. Map entity → DTO, trả về danh sách.
    ///
    /// Error cases:
    ///   - Status không hợp lệ (VD: "BUSY") → BusinessRuleViolationException (422).
    /// </summary>
    public async Task<List<TableResponse>> GetAllAsync(string? status, int? capacity)
    {
        if (status != null && !Enum.TryParse<TableStatus>(status, true, out _))
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.TABLE_STATUS_INVALID, string.Join(", ", Enum.GetNames<TableStatus>())));

        var tables = await _uow.Tables.GetFilteredAsync(status, capacity);
        return tables.Select(MapToResponse).ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // API 2: POST /api/v1/tables/{id}/scan
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Xử lý khi khách quét mã QR trên bàn ăn.
    ///
    /// Luồng xử lý:
    ///   1. Tìm bàn theo ID → 404 nếu không tồn tại.
    ///   2. Kiểm tra bàn có đang bảo trì hoặc đã đặt trước → 422 nếu không phục vụ được.
    ///   3. Nếu có CustomerId → validate customer tồn tại trong DB.
    ///   4. Tìm DiningSession ACTIVE tại bàn này:
    ///      a. Đã có session → trả về session hiện tại (IsNewSession = false).
    ///         Khách mới sẽ tham gia nhóm gọi món cùng bàn.
    ///      b. Chưa có session → tạo DiningSession mới, chuyển bàn sang OCCUPIED.
    ///         Đây là khách đầu tiên ngồi vào bàn (IsNewSession = true).
    ///   5. SaveChanges → commit cả session mới + status bàn trong 1 transaction.
    ///
    /// Error cases:
    ///   - Bàn không tồn tại → EntityNotFoundException (404).
    ///   - Bàn đang MAINTENANCE → BusinessRuleViolationException (422).
    ///   - Bàn đang RESERVED → BusinessRuleViolationException (422).
    ///   - CustomerId không tồn tại → EntityNotFoundException (404).
    /// </summary>
    public async Task<ScanTableResponse> ScanTableAsync(int tableId, ScanTableRequest request)
    {
        var table = await _uow.Tables.GetByIdAsync(tableId)
            ?? throw new EntityNotFoundException("Table", tableId);

        if (table.Status == TableStatus.MAINTENANCE)
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.TABLE_MAINTENANCE_CANNOT_SERVE, table.TableNumber));

        if (table.Status == TableStatus.RESERVED)
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.TABLE_RESERVED, table.TableNumber));

        if (request.CustomerId.HasValue)
        {
            _ = await _uow.Customers.GetByIdAsync(request.CustomerId.Value)
                ?? throw new EntityNotFoundException("Customer", request.CustomerId.Value);
        }

        // Kiểm tra bàn đã có phiên ăn đang hoạt động chưa
        var existingSession = await _uow.DiningSessions.GetActiveByTableIdAsync(tableId);

        if (existingSession != null)
        {
            // Bàn đã có khách → tham gia nhóm gọi món hiện tại
            return new ScanTableResponse
            {
                SessionId = existingSession.Id,
                TableId = tableId,
                Status = existingSession.Status.ToString(),
                IsNewSession = false,
                Message = string.Format(ValidationMessages.SCAN_JOINED_SESSION, table.TableNumber)
            };
        }

        // Bàn trống → tạo phiên ăn mới + chuyển bàn sang OCCUPIED
        var newSession = new DiningSession
        {
            CustomerId = request.CustomerId,
            TableId = tableId,
            Status = DiningSessionStatus.ACTIVE,
            StartedAt = DateTime.UtcNow
        };

        await _uow.DiningSessions.AddAsync(newSession);
        table.Status = TableStatus.OCCUPIED;
        await _uow.SaveChangesAsync();

        return new ScanTableResponse
        {
            SessionId = newSession.Id,
            TableId = tableId,
            Status = newSession.Status.ToString(),
            IsNewSession = true,
            Message = string.Format(ValidationMessages.SCAN_NEW_SESSION, table.TableNumber)
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // API 3: PATCH /api/v1/tables/{id}/status
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Nhân viên cập nhật trạng thái bàn thủ công (VD: sau khi dọn bàn xong → AVAILABLE).
    ///
    /// Luồng xử lý:
    ///   1. Tìm bàn theo ID → 404 nếu không tồn tại.
    ///   2. Validate status mới phải thuộc enum TableStatus.
    ///   3. Side-effect khi chuyển OCCUPIED → AVAILABLE:
    ///      Tự động đóng DiningSession đang ACTIVE (set CLOSED + EndedAt).
    ///      Thực tế: nhân viên dọn bàn xong → bấm AVAILABLE → hệ thống kết thúc phiên ăn.
    ///   4. Block chuyển MAINTENANCE → OCCUPIED (bàn hỏng không thể nhận khách).
    ///   5. Cập nhật status + SaveChanges.
    ///
    /// Error cases:
    ///   - Bàn không tồn tại → EntityNotFoundException (404).
    ///   - Status không hợp lệ → BusinessRuleViolationException (422).
    ///   - Chuyển từ MAINTENANCE → OCCUPIED → BusinessRuleViolationException (422).
    /// </summary>
    public async Task<UpdateTableStatusResponse> UpdateStatusAsync(int id, string status)
    {
        var table = await _uow.Tables.GetByIdAsync(id)
            ?? throw new EntityNotFoundException("Table", id);

        if (!Enum.TryParse<TableStatus>(status, true, out var newStatus))
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.TABLE_STATUS_INVALID, string.Join(", ", Enum.GetNames<TableStatus>())));

        var currentStatus = table.Status;

        // Side-effect: đóng phiên ăn khi chuyển OCCUPIED → AVAILABLE
        if (newStatus == TableStatus.AVAILABLE && currentStatus == TableStatus.OCCUPIED)
        {
            var activeSession = await _uow.DiningSessions.GetActiveByTableIdAsync(id);
            if (activeSession != null)
            {
                activeSession.Status = DiningSessionStatus.CLOSED;
                activeSession.EndedAt = DateTime.UtcNow;
            }
        }

        // Bàn bảo trì không thể nhận khách trực tiếp
        if (newStatus == TableStatus.OCCUPIED && currentStatus == TableStatus.MAINTENANCE)
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.TABLE_MAINTENANCE_CANNOT_OCCUPIED, table.TableNumber));

        table.Status = newStatus;
        await _uow.SaveChangesAsync();

        return new UpdateTableStatusResponse
        {
            TableId = table.Id,
            Status = table.Status.ToString(),
            UpdatedAt = table.UpdatedAt ?? DateTime.UtcNow
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // API 4: POST /api/v1/tables/reservations
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Tạo lịch đặt bàn trước cho khách thành viên hoặc nhân viên nhập hộ khách vãng lai.
    ///
    /// Luồng xử lý:
    ///   1. Tìm bàn theo TableId → 404 nếu không tồn tại.
    ///   2. Validate nghiệp vụ:
    ///      - Bàn không đang bảo trì.
    ///      - PartySize > 0 và không vượt sức chứa bàn.
    ///      - ReservationTime phải ở tương lai (không đặt cho quá khứ).
    ///      - Không trùng lịch với reservation khác trong ±2 giờ.
    ///      - Phải có CustomerId hoặc GuestName (biết ai đặt).
    ///   3. Nếu có CustomerId → validate customer tồn tại.
    ///   4. Tạo entity TableReservation với Status = PENDING.
    ///   5. SaveChanges → trả về ReservationResponse.
    ///
    /// Error cases:
    ///   - Bàn không tồn tại → EntityNotFoundException (404).
    ///   - Bàn đang bảo trì → BusinessRuleViolationException (422).
    ///   - PartySize ≤ 0 → BusinessRuleViolationException (422).
    ///   - PartySize > capacity → BusinessRuleViolationException (422).
    ///   - Thời gian quá khứ → BusinessRuleViolationException (422).
    ///   - Trùng lịch ±2h → BusinessRuleViolationException (422).
    ///   - Customer không tồn tại → EntityNotFoundException (404).
    ///   - Thiếu cả CustomerId lẫn GuestName → BusinessRuleViolationException (422).
    /// </summary>
    public async Task<ReservationResponse> CreateReservationAsync(CreateReservationRequest request)
    {
        var table = await _uow.Tables.GetByIdAsync(request.TableId)
            ?? throw new EntityNotFoundException("Table", request.TableId);

        if (table.Status == TableStatus.MAINTENANCE)
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.RESERVATION_TABLE_MAINTENANCE, table.TableNumber));

        if (request.PartySize <= 0)
            throw new BusinessRuleViolationException(ValidationMessages.RESERVATION_PARTY_SIZE_INVALID);

        if (request.PartySize > table.Capacity)
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.RESERVATION_PARTY_SIZE_EXCEED, table.TableNumber, table.Capacity, request.PartySize));

        if (request.ReservationTime <= DateTime.UtcNow)
            throw new BusinessRuleViolationException(ValidationMessages.RESERVATION_TIME_PAST);

        // Kiểm tra xung đột: có reservation nào trong ±2h không?
        var conflicting = await _uow.TableReservations.GetActiveByTableAndTimeAsync(request.TableId, request.ReservationTime);
        if (conflicting.Count > 0)
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.RESERVATION_TIME_CONFLICT, table.TableNumber));

        if (request.CustomerId.HasValue)
        {
            _ = await _uow.Customers.GetByIdAsync(request.CustomerId.Value)
                ?? throw new EntityNotFoundException("Customer", request.CustomerId.Value);
        }

        // Phải biết ai đặt: hoặc là member (CustomerId) hoặc khách vãng lai (GuestName)
        if (!request.CustomerId.HasValue && string.IsNullOrWhiteSpace(request.GuestName))
            throw new BusinessRuleViolationException(ValidationMessages.RESERVATION_GUEST_INFO_REQUIRED);

        var reservation = new TableReservation
        {
            CustomerId = request.CustomerId,
            TableId = request.TableId,
            GuestName = request.GuestName,
            GuestPhone = request.GuestPhone,
            PartySize = request.PartySize,
            ReservationTime = request.ReservationTime,
            Status = ReservationStatus.PENDING,
            Notes = request.Notes,
            ReservedAt = DateTime.UtcNow
        };

        await _uow.TableReservations.AddAsync(reservation);
        await _uow.SaveChangesAsync();

        return new ReservationResponse
        {
            ReservationId = reservation.Id,
            TableId = table.Id,
            TableNumber = table.TableNumber,
            CustomerId = reservation.CustomerId,
            GuestName = reservation.GuestName,
            GuestPhone = reservation.GuestPhone,
            PartySize = reservation.PartySize,
            ReservationTime = reservation.ReservationTime,
            Status = reservation.Status.ToString(),
            Notes = reservation.Notes
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // API 5: PATCH /api/v1/tables/reservations/{id}/status
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Cập nhật trạng thái lịch đặt bàn (CONFIRMED, CHECKED_IN, CANCELLED, NO_SHOW).
    ///
    /// Luồng xử lý:
    ///   1. Tìm reservation theo ID → 404 nếu không tồn tại.
    ///   2. Validate status mới phải thuộc danh sách hợp lệ.
    ///   3. Block nếu reservation đã ở trạng thái kết thúc (CHECKED_IN, CANCELLED, NO_SHOW).
    ///   4. Tìm bàn liên quan → 404 nếu bàn bị xóa.
    ///   5. Side-effect khi CHECKED_IN:
    ///      - Kiểm tra bàn không đang OCCUPIED hoặc MAINTENANCE.
    ///      - Chuyển bàn → OCCUPIED.
    ///      - Tạo DiningSession mới (ACTIVE) để khách bắt đầu gọi món.
    ///      Thực tế: khách đến nhà hàng → nhân viên bấm CHECKED_IN → hệ thống mở bàn.
    ///   6. Cập nhật reservation.Status + SaveChanges.
    ///
    /// Error cases:
    ///   - Reservation không tồn tại → EntityNotFoundException (404).
    ///   - Status không hợp lệ → BusinessRuleViolationException (422).
    ///   - Reservation đã CHECKED_IN / CANCELLED / NO_SHOW → BusinessRuleViolationException (422).
    ///   - Bàn không tồn tại → EntityNotFoundException (404).
    ///   - Check-in bàn đang OCCUPIED → BusinessRuleViolationException (422).
    ///   - Check-in bàn đang MAINTENANCE → BusinessRuleViolationException (422).
    /// </summary>
    public async Task<UpdateReservationStatusResponse> UpdateReservationStatusAsync(int reservationId, string status)
    {
        var reservation = await _uow.TableReservations.GetByIdAsync(reservationId)
            ?? throw new EntityNotFoundException("TableReservation", reservationId);

        if (!Enum.TryParse<ReservationStatus>(status, true, out var newReservationStatus))
            throw new BusinessRuleViolationException(
                string.Format(ValidationMessages.RESERVATION_STATUS_INVALID, string.Join(", ", Enum.GetNames<ReservationStatus>())));

        // Trạng thái kết thúc → không cho phép thay đổi nữa
        if (reservation.Status == ReservationStatus.CHECKED_IN)
            throw new BusinessRuleViolationException(ValidationMessages.RESERVATION_ALREADY_CHECKED_IN);

        if (reservation.Status == ReservationStatus.CANCELLED)
            throw new BusinessRuleViolationException(ValidationMessages.RESERVATION_ALREADY_CANCELLED);

        if (reservation.Status == ReservationStatus.NO_SHOW)
            throw new BusinessRuleViolationException(ValidationMessages.RESERVATION_ALREADY_NO_SHOW);

        var table = await _uow.Tables.GetByIdAsync(reservation.TableId)
            ?? throw new EntityNotFoundException("Table", reservation.TableId);

        string? newTableStatus = null;

        // Side-effect: khi khách đến (CHECKED_IN) → mở bàn + tạo phiên ăn
        if (newReservationStatus == ReservationStatus.CHECKED_IN)
        {
            if (table.Status == TableStatus.OCCUPIED)
                throw new BusinessRuleViolationException(
                    string.Format(ValidationMessages.TABLE_OCCUPIED_CANNOT_CHECKIN, table.TableNumber));

            if (table.Status == TableStatus.MAINTENANCE)
                throw new BusinessRuleViolationException(
                    string.Format(ValidationMessages.TABLE_MAINTENANCE_CANNOT_CHECKIN, table.TableNumber));

            // Chuyển bàn sang OCCUPIED
            table.Status = TableStatus.OCCUPIED;
            newTableStatus = table.Status.ToString();

            // Tạo phiên ăn mới cho nhóm khách đã đặt trước
            var newSession = new DiningSession
            {
                CustomerId = reservation.CustomerId,
                TableId = reservation.TableId,
                GuestName = reservation.GuestName,
                GuestPhone = reservation.GuestPhone,
                Status = DiningSessionStatus.ACTIVE,
                StartedAt = DateTime.UtcNow
            };
            await _uow.DiningSessions.AddAsync(newSession);
        }

        reservation.Status = newReservationStatus;
        await _uow.SaveChangesAsync();

        return new UpdateReservationStatusResponse
        {
            ReservationId = reservation.Id,
            Status = reservation.Status.ToString(),
            TableStatus = newTableStatus
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Map entity Table → DTO TableResponse.
    /// Chỉ lấy các field cần thiết cho client, không expose navigation properties.
    /// </summary>
    private static TableResponse MapToResponse(Table table) => new()
    {
        Id = table.Id,
        TableNumber = table.TableNumber,
        Capacity = table.Capacity,
        Status = table.Status.ToString(),
        QrCode = table.QrCode
    };
}
