using System;
using System.Collections.Generic;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Chuyến giao món của Robot (robot_delivery_batches).
/// </summary>
public class RobotDeliveryBatch : BaseEntity
{
    public int RobotId { get; set; }
    public Robot Robot { get; set; } = null!;

    public int TableId { get; set; }
    public Table Table { get; set; } = null!;

    public DeliveryBatchStatus Status { get; set; } = DeliveryBatchStatus.PENDING;
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public List<RobotDeliveryItem> DeliveryItems { get; set; } = new();
}
