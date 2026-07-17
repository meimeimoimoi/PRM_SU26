namespace SmartDine.Application.DTOs.DiningSessions;

public class SessionParticipantItem
{
    public int? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class SessionParticipantsResponse
{
    public int SessionId { get; set; }
    public int TableNumber { get; set; }
    public List<SessionParticipantItem> Members { get; set; } = new();
}

public class LeaveSessionResponse
{
    public string Message { get; set; } = string.Empty;
    public int? NewHostId { get; set; }
}

public class BillSummaryResponse
{
    public int SessionId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal Tax { get; set; }
    public decimal EstimatedTotal { get; set; }
    /// <summary>Thuế suất đang áp dụng (%), lấy từ RestaurantSettings.</summary>
    public decimal TaxRate { get; set; }
    /// <summary>Phí dịch vụ đang áp dụng (%), lấy từ RestaurantSettings.</summary>
    public decimal ServiceChargeRate { get; set; }
}

public class SessionOrderItemDetail
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
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
