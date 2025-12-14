using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnly.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbnailPathToModulesAndLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                table: "Modules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                table: "Lessons");
        }
    }
}
