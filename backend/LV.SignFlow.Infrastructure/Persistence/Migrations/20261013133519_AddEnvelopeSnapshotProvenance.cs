using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LV.SignFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopeSnapshotProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Envelopes_OrganizationId_Status",
                table: "Envelopes");

            migrationBuilder.DropIndex(
                name: "IX_EnvelopeRecipients_EnvelopeId_RoutingOrder",
                table: "EnvelopeRecipients");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "EnvelopeFields");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "EnvelopeFields");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Templates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentRoutingOrder",
                table: "Envelopes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActivatedAt",
                table: "EnvelopeRecipients",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CanceledAt",
                table: "EnvelopeRecipients",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DueAt",
                table: "EnvelopeRecipients",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "EnvelopeRecipients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "EnvelopeRecipients",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTemplateRecipientRoleId",
                table: "EnvelopeRecipients",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EnvelopeRecipientId",
                table: "EnvelopeFields",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "OwnerType",
                table: "EnvelopeFields",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTemplateFieldId",
                table: "EnvelopeFields",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceTemplateDocumentId",
                table: "EnvelopeDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnvelopeFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvelopeFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    ValueText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ValueNumber = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    ValueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ValueBoolean = table.Column<bool>(type: "bit", nullable: true),
                    FilledByRecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FilledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FilledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvelopeFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvelopeFieldValues_EnvelopeFields_EnvelopeFieldId",
                        column: x => x.EnvelopeFieldId,
                        principalTable: "EnvelopeFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnvelopeFieldValues_EnvelopeRecipients_FilledByRecipientId",
                        column: x => x.FilledByRecipientId,
                        principalTable: "EnvelopeRecipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnvelopeFieldValues_Users_FilledByUserId",
                        column: x => x.FilledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_OrganizationId_Status_CurrentRoutingOrder",
                table: "Envelopes",
                columns: new[] { "OrganizationId", "Status", "CurrentRoutingOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeRecipients_EnvelopeId_RoutingOrder_Status",
                table: "EnvelopeRecipients",
                columns: new[] { "EnvelopeId", "RoutingOrder", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeRecipients_SourceTemplateRecipientRoleId",
                table: "EnvelopeRecipients",
                column: "SourceTemplateRecipientRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeRecipients_UserId_Status",
                table: "EnvelopeRecipients",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeFields_SourceTemplateFieldId",
                table: "EnvelopeFields",
                column: "SourceTemplateFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeDocuments_SourceTemplateDocumentId",
                table: "EnvelopeDocuments",
                column: "SourceTemplateDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeFieldValues_EnvelopeFieldId",
                table: "EnvelopeFieldValues",
                column: "EnvelopeFieldId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeFieldValues_FilledByRecipientId",
                table: "EnvelopeFieldValues",
                column: "FilledByRecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeFieldValues_FilledByUserId",
                table: "EnvelopeFieldValues",
                column: "FilledByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvelopeFieldValues");

            migrationBuilder.DropIndex(
                name: "IX_Envelopes_OrganizationId_Status_CurrentRoutingOrder",
                table: "Envelopes");

            migrationBuilder.DropIndex(
                name: "IX_EnvelopeRecipients_EnvelopeId_RoutingOrder_Status",
                table: "EnvelopeRecipients");

            migrationBuilder.DropIndex(
                name: "IX_EnvelopeRecipients_SourceTemplateRecipientRoleId",
                table: "EnvelopeRecipients");

            migrationBuilder.DropIndex(
                name: "IX_EnvelopeRecipients_UserId_Status",
                table: "EnvelopeRecipients");

            migrationBuilder.DropIndex(
                name: "IX_EnvelopeFields_SourceTemplateFieldId",
                table: "EnvelopeFields");

            migrationBuilder.DropIndex(
                name: "IX_EnvelopeDocuments_SourceTemplateDocumentId",
                table: "EnvelopeDocuments");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "CurrentRoutingOrder",
                table: "Envelopes");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "EnvelopeRecipients");

            migrationBuilder.DropColumn(
                name: "CanceledAt",
                table: "EnvelopeRecipients");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "EnvelopeRecipients");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "EnvelopeRecipients");

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "EnvelopeRecipients");

            migrationBuilder.DropColumn(
                name: "SourceTemplateRecipientRoleId",
                table: "EnvelopeRecipients");

            migrationBuilder.DropColumn(
                name: "OwnerType",
                table: "EnvelopeFields");

            migrationBuilder.DropColumn(
                name: "SourceTemplateFieldId",
                table: "EnvelopeFields");

            migrationBuilder.DropColumn(
                name: "SourceTemplateDocumentId",
                table: "EnvelopeDocuments");

            migrationBuilder.AlterColumn<Guid>(
                name: "EnvelopeRecipientId",
                table: "EnvelopeFields",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "EnvelopeFields",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "EnvelopeFields",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Envelopes_OrganizationId_Status",
                table: "Envelopes",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeRecipients_EnvelopeId_RoutingOrder",
                table: "EnvelopeRecipients",
                columns: new[] { "EnvelopeId", "RoutingOrder" });
        }
    }
}
