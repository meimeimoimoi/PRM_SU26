using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartDine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionParticipantRoleAndGuestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "PasswordResetTokens",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

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
                name: "FK_session_participants_customers_CustomerId",
                table: "session_participants");

            migrationBuilder.DropColumn(
                name: "GuestSessionId",
                table: "session_participants");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "session_participants");

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
                name: "UserType",
                table: "PasswordResetTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

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
