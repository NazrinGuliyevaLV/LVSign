using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LV.SignFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopeCompletionCertificate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateBlobPath",
                table: "Envelopes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CertificateGeneratedAt",
                table: "Envelopes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateHash",
                table: "Envelopes",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateBlobPath",
                table: "Envelopes");

            migrationBuilder.DropColumn(
                name: "CertificateGeneratedAt",
                table: "Envelopes");

            migrationBuilder.DropColumn(
                name: "CertificateHash",
                table: "Envelopes");
        }
    }
}
