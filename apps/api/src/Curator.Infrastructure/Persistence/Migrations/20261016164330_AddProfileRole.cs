using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Curator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "profiles",
                type: "text",
                nullable: false,
                defaultValue: "Member");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "profiles");
        }
    }
}
