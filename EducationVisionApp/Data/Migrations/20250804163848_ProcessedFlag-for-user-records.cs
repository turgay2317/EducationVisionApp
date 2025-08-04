using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationVisionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProcessedFlagforuserrecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProcessed",
                table: "UserLessons",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProcessed",
                table: "UserLessons");
        }
    }
}
