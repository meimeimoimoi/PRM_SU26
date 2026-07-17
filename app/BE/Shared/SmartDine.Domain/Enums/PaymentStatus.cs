namespace SmartDine.Domain.Enums;

public enum PaymentStatus
{
    PENDING,
    SUCCESS,
    FAILED,
    REFUNDED,
    /// <summary>Link PayOS hết hạn 30 phút mà không có webhook confirm — background job tự set.</summary>
    EXPIRED
}
