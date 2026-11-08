using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LV.SignFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalEnvelopeDocumentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalizedAt",
                table: "EnvelopeDocuments",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "EnvelopeDocuments");
        }
    }
}
