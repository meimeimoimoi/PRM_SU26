using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByUserIdAsync(Guid userId);
}
