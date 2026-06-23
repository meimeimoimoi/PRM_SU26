namespace SmartDine.Application.DTOs.Common;

/// <summary>
/// Standard API response wrapper — tất cả endpoint đều trả format này.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(List<string> errors) =>
        new() { Success = false, Errors = errors };

    public static ApiResponse<T> Fail(string error) =>
        new() { Success = false, Errors = new List<string> { error } };
}

/// <summary>
/// Paginated response wrapper.
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

/// <summary>
/// Paginated API response — data + pagination cùng cấp.
/// </summary>
public class PaginatedApiResponse<T>
{
    public bool Success { get; set; }
    public List<T> Data { get; set; } = new();
    public PaginationMeta Pagination { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static PaginatedApiResponse<T> Ok(List<T> data, int total, int page, int totalPages) =>
        new() { Success = true, Data = data, Pagination = new() { Total = total, Page = page, TotalPages = totalPages } };
}

public class PaginationMeta
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
}
