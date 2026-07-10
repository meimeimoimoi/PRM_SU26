namespace SmartDine.Domain.Enums;

public enum DiningSessionStatus
{
    ACTIVE,
    /// <summary>Đang trong quá trình thanh toán — khóa không cho đặt món mới.</summary>
    CHECKOUT,
    CLOSED
}
