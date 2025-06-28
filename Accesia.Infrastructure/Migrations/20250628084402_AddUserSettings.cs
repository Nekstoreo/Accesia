using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accesia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SmsNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PushNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    InAppNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SecurityAlertsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LoginActivityNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PasswordChangeNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AccountUpdateNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SystemAnnouncementsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DeviceActivityNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ProfileVisibility = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    ShowLastLoginTime = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ShowOnlineStatus = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowDataCollection = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowMarketingEmails = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "es"),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "America/Bogota"),
                    DateFormat = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "dd/MM/yyyy"),
                    TimeFormat = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "24h"),
                    TwoFactorAuthEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RequirePasswordChangeOn2FADisable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LogoutOnPasswordChange = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SessionTimeoutMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NOW()"),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");
        }
    }
}
