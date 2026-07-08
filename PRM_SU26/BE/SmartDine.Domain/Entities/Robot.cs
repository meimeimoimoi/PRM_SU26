using System.Collections.Generic;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Robot giao món (robots).
/// </summary>
public class Robot : BaseEntity
{
    public string RobotCode { get; set; } = string.Empty;
    public string RobotName { get; set; } = string.Empty;
    public RobotStatus Status { get; set; } = RobotStatus.AVAILABLE;
    public int BatteryLevel { get; set; } = 100;
    public string? CurrentLocation { get; set; }

    // Navigation
    public List<RobotDeliveryBatch> DeliveryBatches { get; set; } = new();
}
