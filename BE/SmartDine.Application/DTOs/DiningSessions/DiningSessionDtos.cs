namespace SmartDine.Application.DTOs.DiningSessions;

// ─────────────────────────────────────────────────────────────
// API 1: GET /api/v1/dining-sessions/:id/participants
// ─────────────────────────────────────────────────────────────

public class SessionParticipantItem
{
    public int? UserId { get; set; }      // null khi là GUEST
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "HOST" | "MEMBER"
}

public class SessionParticipantsResponse
{
    public int SessionId { get; set; }
    public int TableNumber { get; set; }
    public List<SessionParticipantItem> Members { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────
// API 2: POST /api/v1/dining-sessions/:id/leave
// ─────────────────────────────────────────────────────────────

public class LeaveSessionResponse
{
    public string Message { get; set; } = string.Empty;
    public int? NewHostId { get; set; }  // null nếu không có HOST mới (người cuối rời)
}

// ─────────────────────────────────────────────────────────────
// API 3: GET /api/v1/dining-sessions/:id/bill-summary
// ─────────────────────────────────────────────────────────────

public class BillSummaryResponse
{
    public int SessionId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal EstimatedTotal { get; set; }
}

// ─────────────────────────────────────────────────────────────
// API 4: GET /api/v1/dining-sessions/:id/orders
// ─────────────────────────────────────────────────────────────

public class SessionOrderItemDetail
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty; // OrderDetailStatus
}

public class SessionOrderSummary
{
    public int OrderId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal FinalAmount { get; set; }
    public List<SessionOrderItemDetail> Items { get; set; } = new();
}

public class SessionOrdersResponse
{
    public int SessionId { get; set; }
    public List<SessionOrderSummary> Orders { get; set; } = new();
}
