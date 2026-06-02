namespace SmartDine.Application.DTOs.Menu;

public class CreateMenuItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public int PrepTimeMinutes { get; set; } = 15;
    public int? Calories { get; set; }
    public string? Allergens { get; set; }
}

public class UpdateMenuItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int PrepTimeMinutes { get; set; }
    public int? Calories { get; set; }
    public string? Allergens { get; set; }
}

public class MenuItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsAvailable { get; set; }
    public int PrepTimeMinutes { get; set; }
    public int? Calories { get; set; }
    public string? Allergens { get; set; }
    public double AverageRating { get; set; }
}

public class MenuCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public string? IconUrl { get; set; }
    public int ItemCount { get; set; }
}
