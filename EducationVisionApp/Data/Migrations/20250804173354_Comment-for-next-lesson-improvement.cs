using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationVisionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class Commentfornextlessonimprovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentForNextTeacher",
                table: "Lessons",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentForNextTeacher",
                table: "Lessons");
        }
    }
}
