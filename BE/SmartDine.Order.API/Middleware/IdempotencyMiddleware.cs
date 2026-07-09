using Microsoft.Extensions.Caching.Distributed;

namespace SmartDine.Order.API.Middleware;

/// <summary>
/// Middleware chặn double request cho Payment endpoints.
///
/// Vấn đề giải quyết: khách bấm "Thanh toán" 2 lần (double-click / retry mạng)
/// → tạo 2 hóa đơn trùng cho cùng 1 phiên ăn.
///
/// Cơ chế:
///   Client gửi header "Idempotency-Key: {uuid}" trong request đầu tiên.
///   Middleware cache response (5 phút) theo key đó.
///   Request thứ 2 cùng key → trả lại response đã cache, không xử lý lại.
///
/// Scope: chỉ áp dụng cho POST /api/v1/payments/* (create-intent).
///        Webhook không dùng Idempotency-Key (PayOS tự retry, service tự idempotent qua PaymentStatus check).
///
/// Yêu cầu: IDistributedCache đã đăng ký (AddDistributedMemoryCache trong Program.cs).
/// Ai dùng: đăng ký trong Program.cs trước ExceptionHandlingMiddleware.
/// </summary>
public class IdempotencyMiddleware
{
    private const string HeaderKey = "Idempotency-Key";
    private const string CachePrefix = "idempotency:payment:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly RequestDelegate _next;
    private readonly IDistributedCache _cache;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IDistributedCache cache,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Chỉ áp dụng cho POST /api/v1/payments
        if (!ShouldApply(context))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[HeaderKey].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            // Không có key → cho qua bình thường (không bắt buộc client gửi key)
            await _next(context);
            return;
        }

        var cacheKey = CachePrefix + idempotencyKey;

        // Kiểm tra cache: đã có response cho key này chưa?
        var cachedResponse = await _cache.GetStringAsync(cacheKey);
        if (cachedResponse != null)
        {
            _logger.LogInformation("Idempotency hit: key={Key}", idempotencyKey);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(cachedResponse);
            return;
        }

        // Buffer response để có thể cache lại
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        // Chỉ cache response thành công (2xx)
        buffer.Position = 0;
        var body = await new StreamReader(buffer).ReadToEndAsync();

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            await _cache.SetStringAsync(cacheKey, body, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl
            });
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody);
    }

    private static bool ShouldApply(HttpContext ctx) =>
        ctx.Request.Method == HttpMethods.Post &&
        ctx.Request.Path.StartsWithSegments("/api/v1/payments");
}
