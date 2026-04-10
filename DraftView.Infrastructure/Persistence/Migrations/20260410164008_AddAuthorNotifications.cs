using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthorNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorNotifications_AuthorId_OccurredAt",
                table: "AuthorNotifications",
                columns: new[] { "AuthorId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorNotifications");
        }
    }
}
