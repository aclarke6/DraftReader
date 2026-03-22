using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrivenerSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScrivenerRootUuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScrivenerRootUuid",
                table: "Projects",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScrivenerRootUuid",
                table: "Projects");
        }
    }
}
