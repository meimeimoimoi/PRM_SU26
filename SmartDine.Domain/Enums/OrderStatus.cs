namespace SmartDine.Domain.Enums;

/// <summary>
/// Trạng thái của đơn hàng trong nhà hàng.
/// </summary>
public enum OrderStatus
{
    /// <summary>Đơn hàng đang chờ xử lý.</summary>
    PENDING,

    /// <summary>Đang chế biến.</summary>
    COOKING,

    /// <summary>Đã hoàn thành.</summary>
    COMPLETED
}
