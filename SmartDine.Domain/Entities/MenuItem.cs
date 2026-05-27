namespace SmartDine.Domain.Entities;

/// <summary>
/// Món ăn trong thực đơn nhà hàng, chưa phải entity thật.
/// </summary>
public class MenuItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
