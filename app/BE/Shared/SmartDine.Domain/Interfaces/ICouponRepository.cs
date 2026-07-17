using SmartDine.Domain.Entities;
using System.Threading.Tasks;

namespace SmartDine.Domain.Interfaces;

public interface ICouponRepository : IRepository<CustomerCoupon>
{
    Task<Promotion?> GetActivePromotionByCodeAsync(string code);
    Task<CustomerCoupon?> GetByCustomerAndPromotionAsync(int customerId, int promotionId);
}
