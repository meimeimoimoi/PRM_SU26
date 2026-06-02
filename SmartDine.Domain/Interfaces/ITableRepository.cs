using SmartDine.Domain.Entities;
using SmartDine.Domain.Enums;

namespace SmartDine.Domain.Interfaces;

public interface ITableRepository : IRepository<Table>
{
    Task<IReadOnlyList<Table>> GetByStatusAsync(TableStatus status);
    Task<Table?> GetByTableNumberAsync(int tableNumber);
}
