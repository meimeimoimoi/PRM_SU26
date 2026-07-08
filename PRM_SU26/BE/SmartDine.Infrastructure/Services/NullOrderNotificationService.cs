using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Services;

/// <summary>
/// Dummy implementation of IOrderNotificationService to satisfy DI requirements in non-Order microservices.
/// </summary>
public class NullOrderNotificationService : IOrderNotificationService
{
    public Task NotifyNewOrderAsync(int orderId, int tableNumber, decimal totalAmount)
    {
        return Task.CompletedTask;
    }

    public Task NotifyOrderStatusChangedAsync(int orderId, int tableId, string status)
    {
        return Task.CompletedTask;
    }
}
