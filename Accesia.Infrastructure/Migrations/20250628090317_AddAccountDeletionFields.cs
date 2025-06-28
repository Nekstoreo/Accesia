using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accesia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountDeletionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountDeletionToken",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountDeletionTokenExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarkedForDeletionAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountDeletionToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccountDeletionTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MarkedForDeletionAt",
                table: "Users");
        }
    }
}
