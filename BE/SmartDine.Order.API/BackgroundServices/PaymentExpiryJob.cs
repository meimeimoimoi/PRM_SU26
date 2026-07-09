using SmartDine.Domain.Enums;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Order.API.BackgroundServices;

/// <summary>
/// Background job tự động xử lý payment hết hạn.
///
/// Vấn đề giải quyết:
///   PayOS payment link có hiệu lực 30 phút. Nếu khách không quét QR và không có webhook FAIL
///   (ví dụ: thoát app, mất mạng), session bị kẹt ở CHECKOUT mãi mãi — không đặt thêm món được,
///   không tạo lại QR được. Background job chạy mỗi 5 phút để detect và xử lý các trường hợp này.
///
/// Hành động khi phát hiện payment PENDING quá hạn:
///   - payment.Status → EXPIRED
///   - session.Status → ACTIVE (unblock đặt món, cho phép retry thanh toán)
///
/// Cách đăng ký: builder.Services.AddHostedService&lt;PaymentExpiryJob&gt;() trong Program.cs.
///
/// Lưu ý: IUnitOfWork là scoped service, BackgroundService là singleton.
///   → Phải tạo scope mới mỗi lần chạy qua IServiceScopeFactory (không inject trực tiếp).
/// </summary>
public class PaymentExpiryJob : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PaymentLinkTtl = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentExpiryJob> _logger;

    public PaymentExpiryJob(IServiceScopeFactory scopeFactory, ILogger<PaymentExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Vòng lặp chính: delay RunInterval → chạy expiry check → lặp lại.
    /// Delay đầu tiên trước khi chạy để tránh xung đột lúc app khởi động.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentExpiryJob started — runs every {Interval} minutes.", RunInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(RunInterval, stoppingToken);

            try
            {
                await ExpireStalePaymentsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log lỗi nhưng tiếp tục chạy — không để 1 lần lỗi dừng toàn bộ job
                _logger.LogError(ex, "PaymentExpiryJob encountered an error during expiry check.");
            }
        }
    }

    /// <summary>
    /// Quét tất cả payment PENDING tạo trước cutoff → mark EXPIRED → unblock session.
    ///
    /// Ai dùng: ExecuteAsync (nội bộ background job).
    /// Side effect: session.Status → ACTIVE cho phép khách retry thanh toán.
    /// </summary>
    private async Task ExpireStalePaymentsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var cutoff = DateTime.UtcNow - PaymentLinkTtl;
        var stalePayments = await uow.Payments.GetPendingOlderThanAsync(cutoff);

        if (!stalePayments.Any()) return;

        _logger.LogInformation("PaymentExpiryJob: found {Count} expired payments to process.", stalePayments.Count);

        foreach (var payment in stalePayments)
        {
            payment.PaymentStatus = PaymentStatus.EXPIRED;
            await uow.Payments.UpdateAsync(payment);

            // Revert session về ACTIVE nếu đang bị khóa ở CHECKOUT
            if (payment.Session?.Status == DiningSessionStatus.CHECKOUT)
            {
                payment.Session.Status = DiningSessionStatus.ACTIVE;
                await uow.DiningSessions.UpdateAsync(payment.Session);
                _logger.LogInformation(
                    "PaymentExpiryJob: session {SessionId} reverted to ACTIVE (payment {InvoiceId} expired).",
                    payment.SessionId, payment.InvoiceId);
            }
        }

        await uow.SaveChangesAsync(ct);
    }
}
