namespace SmartDine.Application.DTOs.Menu;

// ═══════════════════════════════════════════════════════════════
// Requests
// ═══════════════════════════════════════════════════════════════

public class CreateMenuItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
}

public class UpdateMenuItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class PatchMenuItemRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsAvailable { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// Responses
// ═══════════════════════════════════════════════════════════════

public class MenuItemSummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
}

public class MenuItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsAvailable { get; set; }
    public double AverageRating { get; set; }
}

public class MenuItemDetailResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsAvailable { get; set; }
    public double AverageRating { get; set; }
    public int TotalViews { get; set; }
    public List<ReviewSummaryResponse> Reviews { get; set; } = new();
}

public class ReviewSummaryResponse
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MenuItemCreatedResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class MenuItemUpdatedResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AiRecommendedItemResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Reason { get; set; }
}

public class AiRecommendationResponse
{
    public string RecommendationId { get; set; } = string.Empty;
    public List<AiRecommendedItemResponse> Data { get; set; } = new();
}

public class MenuCategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ItemCount { get; set; }
}
