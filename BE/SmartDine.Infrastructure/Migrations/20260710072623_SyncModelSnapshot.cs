using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payments_orders_OrderId",
                table: "payments");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "SUCCESS");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "payments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Deeplink",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalRef",
                table: "payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceId",
                table: "payments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QrUrl",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SplitCount",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_payments_ExternalRef",
                table: "payments",
                column: "ExternalRef",
                unique: true,
                filter: "external_ref IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_InvoiceId",
                table: "payments",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_SessionId",
                table: "payments",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_dining_sessions_SessionId",
                table: "payments",
                column: "SessionId",
                principalTable: "dining_sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_orders_OrderId",
                table: "payments",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payments_dining_sessions_SessionId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_orders_OrderId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_ExternalRef",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_InvoiceId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_SessionId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "Deeplink",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ExternalRef",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "QrUrl",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "SplitCount",
                table: "payments");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SUCCESS",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "PENDING");

            migrationBuilder.AlterColumn<int>(
                name: "OrderId",
                table: "payments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_orders_OrderId",
                table: "payments",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
