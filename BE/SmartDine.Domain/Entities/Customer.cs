using System;
using System.Collections.Generic;

namespace SmartDine.Domain.Entities;

/// <summary>
/// Entity đại diện cho khách hàng thành viên (customers).
/// Khách đăng ký tài khoản để tích điểm, đặt bàn trước, xem lịch sử đơn hàng.
///
/// Hệ thống loyalty theo tổng chi tiêu:
///   - BRONZE: mặc định khi mới đăng ký.
///   - SILVER / GOLD / VIP: nâng cấp dựa trên TotalSpent và VisitCount.
///
/// Xác thực: Email + PasswordHash (BCrypt). PasswordHash nullable vì guest có thể
/// được chuyển thành customer sau (upgrade từ phiên ăn vãng lai).
///
/// Quan hệ:
///   - 1:N với DiningSession → lịch sử các lần ăn tại nhà hàng.
///   - 1:N với Review → đánh giá món ăn.
///   - 1:N với TableReservation → lịch đặt bàn trước.
///   - 1:1 với CustomerStatistics → thống kê tổng hợp.
/// </summary>
public class Customer : BaseEntity
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public int LoyaltyPoints { get; set; } = 0;
    public string MembershipLevel { get; set; } = "BRONZE";
    public decimal TotalSpent { get; set; } = 0.00m;
    public int VisitCount { get; set; } = 0;
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public List<DiningSession> DiningSessions { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public List<CustomerActivity> Activities { get; set; } = new();
    public List<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new();
    public List<CustomerCoupon> Coupons { get; set; } = new();
    public List<SessionParticipant> SessionParticipants { get; set; } = new();
    public List<TableReservation> Reservations { get; set; } = new();
    public List<RecommendationLog> RecommendationLogs { get; set; } = new();
    public CustomerStatistics? Statistics { get; set; }
}
