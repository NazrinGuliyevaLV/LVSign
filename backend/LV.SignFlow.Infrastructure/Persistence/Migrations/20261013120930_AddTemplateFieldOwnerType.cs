using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LV.SignFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateFieldOwnerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RecipientRoleId",
                table: "TemplateFields",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "OwnerType",
                table: "TemplateFields",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "PermissionProfiles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "IsSystemProfile", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("30000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 9, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Allows a user to create and send envelopes and use existing templates.", true, true, "Standard Sender", new DateTimeOffset(new DateTime(2026, 9, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { new Guid("30000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 9, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Allows a user to create, edit, use and share templates.", true, true, "Template Manager", new DateTimeOffset(new DateTime(2026, 9, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                table: "PermissionProfilePermissions",
                columns: new[] { "PermissionId", "PermissionProfileId" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000010"), new Guid("30000000-0000-0000-0000-000000000001") },
                    { new Guid("20000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000009"), new Guid("30000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000010"), new Guid("30000000-0000-0000-0000-000000000002") },
                    { new Guid("20000000-0000-0000-0000-000000000011"), new Guid("30000000-0000-0000-0000-000000000002") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000001"), new Guid("30000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000002"), new Guid("30000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000003"), new Guid("30000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000010"), new Guid("30000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000006"), new Guid("30000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000007"), new Guid("30000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000008"), new Guid("30000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000009"), new Guid("30000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000010"), new Guid("30000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "PermissionProfilePermissions",
                keyColumns: new[] { "PermissionId", "PermissionProfileId" },
                keyValues: new object[] { new Guid("20000000-0000-0000-0000-000000000011"), new Guid("30000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "PermissionProfiles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "PermissionProfiles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"));

            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "TemplateFields");

            migrationBuilder.AlterColumn<Guid>(
                name: "RecipientRoleId",
                table: "TemplateFields",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
