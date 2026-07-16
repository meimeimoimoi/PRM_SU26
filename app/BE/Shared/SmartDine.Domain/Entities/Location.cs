using System.Collections.Generic;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Khu vực/vị trí bàn ăn trong nhà hàng (VD: Tầng 1, Sân vườn, Phòng VIP).
/// </summary>
public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation
    public List<Table> Tables { get; set; } = new();
}
