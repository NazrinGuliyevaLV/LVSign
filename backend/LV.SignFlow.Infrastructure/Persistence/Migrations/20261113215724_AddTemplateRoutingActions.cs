using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LV.SignFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateRoutingActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemplateRoutingActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateRoutingRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateRoutingActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateRoutingActions_TemplateRecipientRoles_RecipientRoleId",
                        column: x => x.RecipientRoleId,
                        principalTable: "TemplateRecipientRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateRoutingActions_TemplateRoutingRules_TemplateRoutingRuleId",
                        column: x => x.TemplateRoutingRuleId,
                        principalTable: "TemplateRoutingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRoutingActions_RecipientRoleId",
                table: "TemplateRoutingActions",
                column: "RecipientRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRoutingActions_TemplateRoutingRuleId",
                table: "TemplateRoutingActions",
                column: "TemplateRoutingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRoutingActions_TemplateRoutingRuleId_Order",
                table: "TemplateRoutingActions",
                columns: new[] { "TemplateRoutingRuleId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateRoutingActions");
        }
    }
}
