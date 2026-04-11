using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameNotificationPreferencesToUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationPreferences_AppUsers_UserId",
                table: "NotificationPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NotificationPreferences",
                table: "NotificationPreferences");

            migrationBuilder.RenameTable(
                name: "NotificationPreferences",
                newName: "UserPreferences");

            migrationBuilder.RenameIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "UserPreferences",
                newName: "IX_UserPreferences_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserPreferences",
                table: "UserPreferences",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPreferences_AppUsers_UserId",
                table: "UserPreferences",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPreferences_AppUsers_UserId",
                table: "UserPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserPreferences",
                table: "UserPreferences");

            migrationBuilder.RenameTable(
                name: "UserPreferences",
                newName: "NotificationPreferences");

            migrationBuilder.RenameIndex(
                name: "IX_UserPreferences_UserId",
                table: "NotificationPreferences",
                newName: "IX_NotificationPreferences_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NotificationPreferences",
                table: "NotificationPreferences",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationPreferences_AppUsers_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
