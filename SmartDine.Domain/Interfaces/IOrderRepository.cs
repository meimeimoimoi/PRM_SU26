using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Bản vẽ (contract) cho Order Repository.
/// Quy định các hàm mà bất kỳ implementation nào cũng phải có.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Thêm đơn hàng mới.</summary>
    void Add(Order order);

    /// <summary>Lấy đơn hàng theo Id.</summary>
    Order? GetById(Guid id);

    /// <summary>Lấy tất cả đơn hàng.</summary>
    IEnumerable<Order> GetAll();
}
