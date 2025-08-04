using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationVisionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class Avgrecordsupdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvgConfidence",
                table: "UserLessons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalBlinkCount",
                table: "UserLessons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalHeadTurn",
                table: "UserLessons",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgConfidence",
                table: "UserLessons");

            migrationBuilder.DropColumn(
                name: "TotalBlinkCount",
                table: "UserLessons");

            migrationBuilder.DropColumn(
                name: "TotalHeadTurn",
                table: "UserLessons");
        }
    }
}
