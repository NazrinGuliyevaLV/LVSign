using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LV.SignFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImmutableSigningEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FontKey",
                table: "Signatures",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypedText",
                table: "Signatures",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppliedContentType",
                table: "EnvelopeAppliedSignatures",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FontKeySnapshot",
                table: "EnvelopeAppliedSignatures",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignatureTypeSnapshot",
                table: "EnvelopeAppliedSignatures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypedTextSnapshot",
                table: "EnvelopeAppliedSignatures",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FontKey",
                table: "Signatures");

            migrationBuilder.DropColumn(
                name: "TypedText",
                table: "Signatures");

            migrationBuilder.DropColumn(
                name: "AppliedContentType",
                table: "EnvelopeAppliedSignatures");

            migrationBuilder.DropColumn(
                name: "FontKeySnapshot",
                table: "EnvelopeAppliedSignatures");

            migrationBuilder.DropColumn(
                name: "SignatureTypeSnapshot",
                table: "EnvelopeAppliedSignatures");

            migrationBuilder.DropColumn(
                name: "TypedTextSnapshot",
                table: "EnvelopeAppliedSignatures");
        }
    }
}
