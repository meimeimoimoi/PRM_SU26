namespace SmartDine.Application.DTOs.Common;

/// <summary>Một điểm dữ liệu trên chart theo thời gian (giờ/ngày) — dùng chung cho order-chart và revenue-chart.</summary>
public class ChartPointResponse
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
