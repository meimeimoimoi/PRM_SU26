using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentIndex_V2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payments_orders_OrderId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_session_participants_customers_CustomerId",
                table: "session_participants");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "session_participants",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "GuestSessionId",
                table: "session_participants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "session_participants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "RefreshTokens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "promotions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "PasswordResetTokens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_Code",
                table: "promotions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_ExternalRef",
                table: "payments",
                column: "ExternalRef",
                unique: true,
                filter: "\"ExternalRef\" IS NOT NULL");

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

            migrationBuilder.AddForeignKey(
                name: "FK_session_participants_customers_CustomerId",
                table: "session_participants",
                column: "CustomerId",
                principalTable: "customers",
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

            migrationBuilder.DropForeignKey(
                name: "FK_session_participants_customers_CustomerId",
                table: "session_participants");

            migrationBuilder.DropIndex(
                name: "IX_promotions_Code",
                table: "promotions");

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
                name: "GuestSessionId",
                table: "session_participants");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "session_participants");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "promotions");

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

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "session_participants",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "RefreshTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

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

            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "PasswordResetTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_orders_OrderId",
                table: "payments",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_session_participants_customers_CustomerId",
                table: "session_participants",
                column: "CustomerId",
                principalTable: "customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
