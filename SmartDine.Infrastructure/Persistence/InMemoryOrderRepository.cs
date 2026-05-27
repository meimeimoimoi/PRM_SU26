using SmartDine.Domain.Entities;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.Persistence;

/// <summary>
/// Implementation của IOrderRepository dùng List trong RAM (Mock Data).
/// Sau này có thể thay bằng EF Core / SQL Server mà không cần sửa code ở tầng trên.
/// </summary>
public class InMemoryOrderRepository : IOrderRepository
{
    // Dữ liệu giả lưu trong RAM
    private readonly List<Order> _orders = new();

    public void Add(Order order)
    {
        _orders.Add(order);
    }

    public Order? GetById(Guid id)
    {
        return _orders.FirstOrDefault(o => o.Id == id);
    }

    public IEnumerable<Order> GetAll()
    {
        return _orders.AsReadOnly();
    }
}
