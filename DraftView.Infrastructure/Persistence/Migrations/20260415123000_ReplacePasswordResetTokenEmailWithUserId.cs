using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePasswordResetTokenEmailWithUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "PasswordResetTokens");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PasswordResetTokens",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PasswordResetTokens");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "PasswordResetTokens",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
