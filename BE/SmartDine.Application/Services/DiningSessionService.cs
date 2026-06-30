using SmartDine.Application.Constants;
using SmartDine.Application.DTOs.DiningSessions;
using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;
using SmartDine.Domain.Exceptions;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Application.Services;

/// <summary>
/// Service xử lý nghiệp vụ cho phiên ăn uống đang hoạt động.
///
/// Chịu trách nhiệm:
///   - Xem danh sách thành viên cùng bàn (API 1).
///   - Thực khách rời nhóm gọi món, chuyển HOST nếu cần (API 2).
///   - Tính tổng chi tiêu tạm tính (API 3).
///   - Xem tất cả món đã gọi trong phiên (API 4).
/// </summary>
public class DiningSessionService
{
    private const decimal TaxRate = 0.10m;

    private readonly IUnitOfWork _uow;

    public DiningSessionService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // ═══════════════════════════════════════════════════════════════
    // API 1: GET /api/v1/dining-sessions/{id}/participants
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Lấy danh sách thành viên đang có mặt trong phiên ăn (LeftAt == null).
    ///
    /// Luồng:
    ///   1. Load session kèm Table + Participants.Customer.
    ///   2. Kiểm tra quyền xem: STAFF/MANAGER xem được mọi session; CUSTOMER/GUEST chỉ xem
    ///      được session mà chính họ đang là thành viên.
    ///   3. Lọc những ai chưa rời (LeftAt == null).
    ///   4. Map sang SessionParticipantItem: user_id, name (hoặc "Khách" cho GUEST), role.
    ///
    /// Error cases:
    ///   - Session không tồn tại → EntityNotFoundException (404).
    ///   - CUSTOMER/GUEST gọi nhưng không thuộc session → UnauthorizedAccessException (403).
    /// </summary>
    public async Task<SessionParticipantsResponse> GetParticipantsAsync(
        int sessionId, int? callerCustomerId, string? callerGuestSessionId, bool isStaff)
    {
        var session = await _uow.DiningSessions.GetByIdWithParticipantsAsync(sessionId)
            ?? throw new EntityNotFoundException("DiningSession", sessionId);

        EnsureCallerCanView(session.Participants, callerCustomerId, callerGuestSessionId, isStaff);

        var activeParticipants = session.Participants
            .Where(p => p.IsActive)
            .OrderBy(p => p.JoinedAt)
            .Select((p, index) => new SessionParticipantItem
            {
                UserId = p.CustomerId,
                Name = p.Customer?.FullName ?? $"Khách {index + 1}",
                Role = p.Role.ToString()
            })
            .ToList();

        return new SessionParticipantsResponse
        {
            SessionId = session.Id,
            TableNumber = session.Table.TableNumber,
            Members = activeParticipants
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // API 2: POST /api/v1/dining-sessions/{id}/leave
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Thực khách rời khỏi nhóm gọi món.
    ///
    /// Luồng:
    ///   1. Load session với Participants.
    ///   2. Tìm participant theo customerId (CUSTOMER) hoặc guestSessionId (GUEST).
    ///   3. Kiểm tra chưa rời trước đó.
    ///   4. Set LeftAt = UtcNow.
    ///   5. Nếu là HOST và còn thành viên khác → chuyển HOST cho người JoinedAt sớm nhất còn lại.
    ///   6. SaveChanges.
    ///
    /// Error cases:
    ///   - Session không tồn tại → 404.
    ///   - Người gọi không thuộc session → 422.
    ///   - Đã rời trước đó → 422.
    /// </summary>
    public async Task<LeaveSessionResponse> LeaveSessionAsync(
        int sessionId,
        int? customerId,
        string? guestSessionId)
    {
        var session = await _uow.DiningSessions.GetByIdWithParticipantsAsync(sessionId)
            ?? throw new EntityNotFoundException("DiningSession", sessionId);

        var participant = FindParticipant(session.Participants, customerId, guestSessionId)
            ?? throw new BusinessRuleViolationException(ValidationMessages.DINING_SESSION_PARTICIPANT_NOT_FOUND);

        if (!participant.IsActive)
            throw new BusinessRuleViolationException(ValidationMessages.DINING_SESSION_ALREADY_LEFT);

        participant.LeftAt = DateTime.UtcNow;

        int? newHostId = null;

        // Chuyển quyền HOST nếu người rời là HOST
        if (participant.Role == ParticipantRole.HOST)
        {
            var nextHost = session.Participants
                .Where(p => p.Id != participant.Id && p.IsActive)
                .OrderBy(p => p.JoinedAt)
                .FirstOrDefault();

            if (nextHost != null)
            {
                nextHost.Role = ParticipantRole.HOST;
                newHostId = nextHost.CustomerId;
                await _uow.SessionParticipants.UpdateAsync(nextHost);
            }
        }

        await _uow.SessionParticipants.UpdateAsync(participant);
        await _uow.SaveChangesAsync();

        return new LeaveSessionResponse
        {
            Message = string.Format(
                ValidationMessages.DINING_SESSION_LEAVE_SUCCESS,
                session.Table.TableNumber),
            NewHostId = newHostId
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // API 3: GET /api/v1/dining-sessions/{id}/bill-summary
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Tính tổng chi tiêu tạm tính của phiên ăn.
    ///
    /// Luồng:
    ///   1. Load session kèm Participants.
    ///   2. Kiểm tra quyền xem (giống API 1).
    ///   3. Lấy tất cả orders của session (không tính CANCELLED).
    ///   4. sub_total = tổng FinalAmount.
    ///   5. tax = sub_total * 10% (VAT).
    ///   6. estimated_total = sub_total + tax.
    ///
    /// Error cases:
    ///   - Session không tồn tại → 404.
    ///   - CUSTOMER/GUEST gọi nhưng không thuộc session → UnauthorizedAccessException (403).
    /// </summary>
    public async Task<BillSummaryResponse> GetBillSummaryAsync(
        int sessionId, int? callerCustomerId, string? callerGuestSessionId, bool isStaff)
    {
        var session = await _uow.DiningSessions.GetByIdWithParticipantsAsync(sessionId)
            ?? throw new EntityNotFoundException("DiningSession", sessionId);

        EnsureCallerCanView(session.Participants, callerCustomerId, callerGuestSessionId, isStaff);

        var orders = await _uow.Orders.GetByDiningSessionIdAsync(sessionId);

        var subTotal = orders
            .Where(o => o.Status != OrderStatus.CANCELLED)
            .Sum(o => o.FinalAmount);

        var tax = Math.Round(subTotal * TaxRate, 2);

        return new BillSummaryResponse
        {
            SessionId = sessionId,
            SubTotal = subTotal,
            Tax = tax,
            EstimatedTotal = subTotal + tax
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // API 4: GET /api/v1/dining-sessions/{id}/orders
    // ═══════════════════════════════════════════════════════════════
    /// <summary>
    /// Lấy toàn bộ orders trong phiên ăn kèm chi tiết từng món.
    ///
    /// Luồng:
    ///   1. Validate session tồn tại, load kèm Participants.
    ///   2. Kiểm tra quyền xem (giống API 1).
    ///   3. Load orders + OrderDetails + MenuItem.
    ///   4. Map sang SessionOrderSummary với status từng item.
    ///
    /// Error cases:
    ///   - Session không tồn tại → 404.
    ///   - CUSTOMER/GUEST gọi nhưng không thuộc session → UnauthorizedAccessException (403).
    /// </summary>
    public async Task<SessionOrdersResponse> GetSessionOrdersAsync(
        int sessionId, int? callerCustomerId, string? callerGuestSessionId, bool isStaff)
    {
        var session = await _uow.DiningSessions.GetByIdWithParticipantsAsync(sessionId)
            ?? throw new EntityNotFoundException("DiningSession", sessionId);

        EnsureCallerCanView(session.Participants, callerCustomerId, callerGuestSessionId, isStaff);

        var orders = await _uow.Orders.GetByDiningSessionIdAsync(sessionId);

        return new SessionOrdersResponse
        {
            SessionId = sessionId,
            Orders = orders.Select(o => new SessionOrderSummary
            {
                OrderId = o.Id,
                OrderStatus = o.Status.ToString(),
                FinalAmount = o.FinalAmount,
                Items = o.OrderDetails.Select(d => new SessionOrderItemDetail
                {
                    Name = d.MenuItem?.Name ?? "Unknown",
                    Quantity = d.Quantity,
                    Status = d.Status.ToString()
                }).ToList()
            }).ToList()
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Chặn truy cập trái phép: STAFF/MANAGER xem được mọi session; CUSTOMER/GUEST chỉ xem
    /// được session mà chính họ đang là thành viên đang hoạt động (chưa rời).
    /// </summary>
    private static void EnsureCallerCanView(
        IEnumerable<SessionParticipant> participants,
        int? callerCustomerId,
        string? callerGuestSessionId,
        bool isStaff)
    {
        if (isStaff)
            return;

        var isParticipant = participants.Any(p =>
            p.IsActive &&
            ((callerCustomerId.HasValue && p.CustomerId == callerCustomerId) ||
             (!string.IsNullOrEmpty(callerGuestSessionId) && p.GuestSessionId == callerGuestSessionId)));

        if (!isParticipant)
            throw new UnauthorizedAccessException(ValidationMessages.DINING_SESSION_ACCESS_DENIED);
    }

    private static SessionParticipant? FindParticipant(
        IEnumerable<SessionParticipant> participants,
        int? customerId,
        string? guestSessionId)
    {
        if (customerId.HasValue)
            return participants.FirstOrDefault(p => p.CustomerId == customerId);

        if (!string.IsNullOrEmpty(guestSessionId))
            return participants.FirstOrDefault(p => p.GuestSessionId == guestSessionId);

        return null;
    }
}
