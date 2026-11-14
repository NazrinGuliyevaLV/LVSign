using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LV.SignFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopeRoutingActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnvelopeRoutingActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnvelopeRoutingRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvelopeRoutingActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvelopeRoutingActions_EnvelopeRecipients_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "EnvelopeRecipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnvelopeRoutingActions_EnvelopeRoutingRules_EnvelopeRoutingRuleId",
                        column: x => x.EnvelopeRoutingRuleId,
                        principalTable: "EnvelopeRoutingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeRoutingActions_EnvelopeRoutingRuleId",
                table: "EnvelopeRoutingActions",
                column: "EnvelopeRoutingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeRoutingActions_EnvelopeRoutingRuleId_Order",
                table: "EnvelopeRoutingActions",
                columns: new[] { "EnvelopeRoutingRuleId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeRoutingActions_EnvelopeRoutingRuleId_RecipientId",
                table: "EnvelopeRoutingActions",
                columns: new[] { "EnvelopeRoutingRuleId", "RecipientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeRoutingActions_RecipientId",
                table: "EnvelopeRoutingActions",
                column: "RecipientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvelopeRoutingActions");
        }
    }
}
