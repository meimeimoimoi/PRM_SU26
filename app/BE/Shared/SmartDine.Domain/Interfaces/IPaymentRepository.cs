using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Repository thanh toán — bổ sung query nghiệp vụ trên GenericRepository.
/// Ai dùng: PaymentService để kiểm tra trùng thanh toán và match webhook.
/// </summary>
public interface IPaymentRepository : IRepository<Payment>
{
    /// <summary>Lấy thanh toán theo OrderId (backward-compat với code cũ).</summary>
    Task<Payment?> GetByOrderIdAsync(int orderId);

    /// <summary>
    /// Lấy số thứ tự tiếp theo từ PostgreSQL sequence để làm orderCode cho PayOS.
    /// Đảm bảo unique tuyệt đối (không thể trùng dù nhiều request song song).
    /// Fallback về timestamp khi dùng InMemory DB (test environment).
    /// </summary>
    Task<long> GetNextOrderCodeAsync();

    /// <summary>
    /// Lấy danh sách payments PENDING đã tạo trước cutoff — phục vụ background job hết hạn.
    /// Dùng bởi PaymentExpiryJob để tự động giải phóng session bị treo.
    /// </summary>
    Task<IReadOnlyList<Payment>> GetPendingOlderThanAsync(DateTime cutoff);

    /// <summary>
    /// Lấy thanh toán mới nhất của 1 phiên ăn.
    /// Dùng khi create-intent: kiểm tra đã có PENDING/SUCCESS chưa để tránh tạo trùng.
    /// </summary>
    Task<Payment?> GetBySessionIdAsync(int sessionId);

    /// <summary>
    /// Tìm thanh toán qua mã tham chiếu cổng thanh toán (PayOS orderCode).
    /// Dùng trong webhook handler để map giao dịch về đúng Payment record.
    /// </summary>
    Task<Payment?> GetByExternalRefAsync(string externalRef);

    Task<IReadOnlyList<Payment>> GetByDateRangeAsync(DateTime start, DateTime end);
}
