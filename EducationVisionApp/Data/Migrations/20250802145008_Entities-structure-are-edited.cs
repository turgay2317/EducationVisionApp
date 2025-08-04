using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationVisionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class Entitiesstructureareedited : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adhd",
                table: "Records");

            migrationBuilder.RenameColumn(
                name: "Stress",
                table: "Records",
                newName: "Sleepy");

            migrationBuilder.RenameColumn(
                name: "Sadness",
                table: "Records",
                newName: "Focused");

            migrationBuilder.RenameColumn(
                name: "Hyperactivity",
                table: "Records",
                newName: "Distracted");

            migrationBuilder.AddColumn<float>(
                name: "AvgDistracted",
                table: "UserClasses",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AvgFocused",
                table: "UserClasses",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AvgSleepy",
                table: "UserClasses",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Classes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsFinished",
                table: "Classes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "Classes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgDistracted",
                table: "UserClasses");

            migrationBuilder.DropColumn(
                name: "AvgFocused",
                table: "UserClasses");

            migrationBuilder.DropColumn(
                name: "AvgSleepy",
                table: "UserClasses");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "IsFinished",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Classes");

            migrationBuilder.RenameColumn(
                name: "Sleepy",
                table: "Records",
                newName: "Stress");

            migrationBuilder.RenameColumn(
                name: "Focused",
                table: "Records",
                newName: "Sadness");

            migrationBuilder.RenameColumn(
                name: "Distracted",
                table: "Records",
                newName: "Hyperactivity");

            migrationBuilder.AddColumn<float>(
                name: "Adhd",
                table: "Records",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
