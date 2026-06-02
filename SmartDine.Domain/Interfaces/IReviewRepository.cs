using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    Task<IReadOnlyList<Review>> GetByMenuItemIdAsync(Guid menuItemId);
    Task<IReadOnlyList<Review>> GetByCustomerIdAsync(Guid customerId);
    Task<double> GetAverageRatingByMenuItemIdAsync(Guid menuItemId);
}
