using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accesia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAccountDeletionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DeletionReason",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AccountDeletionToken",
                table: "Users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AccountDeletionToken",
                table: "Users",
                column: "AccountDeletionToken");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MarkedForDeletionAt",
                table: "Users",
                column: "MarkedForDeletionAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_AccountDeletionToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MarkedForDeletionAt",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "DeletionReason",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AccountDeletionToken",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
