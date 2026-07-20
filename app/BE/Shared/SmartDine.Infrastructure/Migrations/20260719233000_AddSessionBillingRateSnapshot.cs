using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartDine.Infrastructure.Persistence;

#nullable disable

namespace SmartDine.Infrastructure.Migrations
{
    [DbContext(typeof(SmartDineDbContext))]
    [Migration("20260719233000_AddSessionBillingRateSnapshot")]
    public partial class AddSessionBillingRateSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "dining_sessions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceChargeRate",
                table: "dining_sessions",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            // Snapshot cho phiên đang mở: copy settings hiện tại để không đổi giữa chừng
            migrationBuilder.Sql("""
                UPDATE dining_sessions ds
                SET "TaxRate" = s."TaxRate",
                    "ServiceChargeRate" = s."ServiceChargeRate"
                FROM restaurant_settings s
                WHERE ds."Status" IN ('ACTIVE', 'CHECKOUT')
                  AND ds."IsDeleted" = FALSE;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "dining_sessions");

            migrationBuilder.DropColumn(
                name: "ServiceChargeRate",
                table: "dining_sessions");
        }
    }
}
