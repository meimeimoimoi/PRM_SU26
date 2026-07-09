using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSequenceAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Opt 1: PostgreSQL sequence cho PayOS orderCode ──
            // Thay thế timestamp-based generation — đảm bảo unique tuyệt đối dù nhiều request song song.
            // GetNextOrderCodeAsync() gọi SELECT nextval('payment_order_code_seq').
            migrationBuilder.Sql(
                "CREATE SEQUENCE IF NOT EXISTS payment_order_code_seq " +
                "START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;");

            // ── Opt 4: Index trên dining_sessions.status ──
            // Các query hay dùng: GetActiveSessionsAsync() WHERE status='ACTIVE',
            // GetActiveByTableIdAsync() WHERE status='ACTIVE'.
            // Sau khi thêm CHECKOUT, query sẽ có 3 giá trị → index partial cho ACTIVE/CHECKOUT.
            migrationBuilder.CreateIndex(
                name: "IX_dining_sessions_status",
                table: "dining_sessions",
                column: "Status");

            // ── Opt 4: Index trên payments.payment_status ──
            // PaymentExpiryJob query: WHERE payment_status = 'PENDING' AND created_at < cutoff.
            // Partial index trên PENDING giúp skip các SUCCESS/FAILED/EXPIRED records (số lượng lớn hơn nhiều).
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS \"IX_payments_pending_status\" " +
                "ON payments (payment_status, created_at) " +
                "WHERE payment_status = 'PENDING';");

            // ── Opt 4: Index trên payments.session_id ──
            // GetBySessionIdAsync() — kiểm tra existing payment trước khi create-intent.
            // Đã tạo ở migration trước (IX_payments_SessionId) nên skip ở đây.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS payment_order_code_seq;");
            migrationBuilder.DropIndex(name: "IX_dining_sessions_status", table: "dining_sessions");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_payments_pending_status\";");
        }
    }
}
