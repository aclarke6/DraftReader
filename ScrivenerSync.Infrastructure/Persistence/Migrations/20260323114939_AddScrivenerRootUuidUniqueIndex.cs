using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrivenerSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScrivenerRootUuidUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Projects_ScrivenerRootUuid",
                table: "Projects",
                column: "ScrivenerRootUuid",
                unique: true,
                filter: "\"ScrivenerRootUuid\" IS NOT NULL");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_ScrivenerRootUuid",
                table: "Projects");

        }
    }
}

