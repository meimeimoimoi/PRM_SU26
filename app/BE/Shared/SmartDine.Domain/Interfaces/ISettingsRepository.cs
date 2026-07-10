using SmartDine.Domain.Entities;

namespace SmartDine.Domain.Interfaces;

/// <summary>
/// Repository cho cấu hình nhà hàng — luôn chỉ có 1 bản ghi (singleton row).
/// </summary>
public interface ISettingsRepository : IRepository<RestaurantSettings>
{
    /// <summary>
    /// Trả về bản ghi cấu hình duy nhất (Id nhỏ nhất). Nếu bảng rỗng, tạo bản ghi mặc định.
    /// </summary>
    Task<RestaurantSettings> GetSingletonAsync();
}
