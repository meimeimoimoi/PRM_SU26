using System;
using System.Collections.Generic;
using System.Linq;
using SmartDine.Application.DTOs.Common;

namespace SmartDine.Application.Helper;

/// <summary>
/// Gộp dữ liệu (timestamp, amount) thành các điểm chart theo period — dùng chung cho
/// order-chart (OrderService) và revenue-chart (PaymentService) trên Dashboard Manager.
/// </summary>
public static class ChartPeriodHelper
{
    /// <summary>
    /// Khoảng thời gian cần query từ DB cho period tương ứng.
    ///   day   → hôm nay (00:00 → hiện tại).
    ///   week  → 7 ngày gần nhất kể cả hôm nay.
    ///   month → từ đầu tháng hiện tại → hiện tại.
    /// period không hợp lệ → mặc định "day".
    /// </summary>
    public static (DateTime Start, DateTime End) ResolveRange(string? period)
    {
        var now = DateTime.UtcNow;
        return period?.ToLowerInvariant() switch
        {
            "week" => (now.Date.AddDays(-6), now),
            "month" => (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), now),
            _ => (now.Date, now)
        };
    }

    /// <summary>
    /// Gộp data vào bucket theo period:
    ///   day   → 24 bucket theo giờ trong ngày hôm nay, label "HH:00".
    ///   week  → 7 bucket theo ngày, label "dd/MM".
    ///   month → 1 bucket/ngày từ đầu tháng đến hôm nay, label "dd/MM".
    /// Bucket rỗng vẫn xuất hiện với Value = 0 (giữ trục thời gian liên tục cho chart).
    /// </summary>
    public static List<ChartPointResponse> Bucket(IEnumerable<(DateTime Timestamp, decimal Amount)> data, string? period)
    {
        var now = DateTime.UtcNow;
        var normalizedPeriod = period?.ToLowerInvariant();

        if (normalizedPeriod == "week")
            return BucketByDay(data, now.Date.AddDays(-6), 7);

        if (normalizedPeriod == "month")
        {
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var dayCount = (now.Date - monthStart).Days + 1;
            return BucketByDay(data, monthStart, dayCount);
        }

        // "day" mặc định — 24 bucket theo giờ trong ngày hôm nay
        var hourBuckets = Enumerable.Range(0, 24).ToDictionary(h => h, _ => 0m);
        foreach (var (timestamp, amount) in data)
        {
            if (timestamp.Date == now.Date)
                hourBuckets[timestamp.Hour] += amount;
        }

        return hourBuckets
            .Select(b => new ChartPointResponse { Label = $"{b.Key:D2}:00", Value = b.Value })
            .ToList();
    }

    private static List<ChartPointResponse> BucketByDay(
        IEnumerable<(DateTime Timestamp, decimal Amount)> data, DateTime start, int dayCount)
    {
        var buckets = Enumerable.Range(0, dayCount)
            .Select(i => start.AddDays(i))
            .ToDictionary(d => d, _ => 0m);

        foreach (var (timestamp, amount) in data)
        {
            var day = timestamp.Date;
            if (buckets.ContainsKey(day))
                buckets[day] += amount;
        }

        return buckets
            .OrderBy(b => b.Key)
            .Select(b => new ChartPointResponse { Label = b.Key.ToString("dd/MM"), Value = b.Value })
            .ToList();
    }
}
