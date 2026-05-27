namespace SmartDine.Application.DTOs;

/// <summary>
/// DTO hứng dữ liệu từ Flutter/Frontend gửi lên khi đặt món.
/// </summary>
public class PlaceOrderRequest
{
    /// <summary>Danh sách Id các món ăn được đặt.</summary>
    public List<Guid> MenuItemIds { get; set; } = new();
}
