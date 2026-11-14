using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LV.SignFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteTemplateWorkFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpirationDays",
                table: "TemplateVersions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderIntervalDays",
                table: "TemplateVersions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "TemplateFields",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionJson",
                table: "TemplateFields",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpirationDays",
                table: "TemplateVersions");

            migrationBuilder.DropColumn(
                name: "ReminderIntervalDays",
                table: "TemplateVersions");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "TemplateFields");

            migrationBuilder.DropColumn(
                name: "OptionJson",
                table: "TemplateFields");
        }
    }
}
