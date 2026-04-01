using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftView.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorIdAndDropboxConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add AuthorId as nullable with no FK yet
            migrationBuilder.AddColumn<Guid>(
                name: "AuthorId",
                table: "Projects",
                type: "uuid",
                nullable: true);

            // Step 2: Backfill â€” set AuthorId to the single Author user's Id
            migrationBuilder.Sql(@"
                UPDATE ""Projects""
                SET ""AuthorId"" = (
                    SELECT ""Id"" FROM ""AppUsers""
                    WHERE ""Role"" = 'Author'
                    LIMIT 1
                )
                WHERE ""AuthorId"" IS NULL;
            ");

            // Step 3: Make non-nullable now that all rows are backfilled
            migrationBuilder.AlterColumn<Guid>(
                name: "AuthorId",
                table: "Projects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Step 4: Add FK constraint
            migrationBuilder.CreateIndex(
                name: "IX_Projects_AuthorId",
                table: "Projects",
                column: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AppUsers_AuthorId",
                table: "Projects",
                column: "AuthorId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Step 5: Create DropboxConnections table
            migrationBuilder.CreateTable(
                name: "DropboxConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RefreshToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuthorisedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DropboxConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DropboxConnections_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DropboxConnections_UserId",
                table: "DropboxConnections",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DropboxConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AppUsers_AuthorId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_AuthorId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Projects");
        }
    }
}
