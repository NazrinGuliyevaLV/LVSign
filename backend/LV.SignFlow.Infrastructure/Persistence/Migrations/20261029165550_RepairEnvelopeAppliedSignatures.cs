using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LV.SignFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RepairEnvelopeAppliedSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnvelopeAppliedSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    EnvelopeId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    EnvelopeFieldId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    RecipientId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    AppliedByUserId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: false),

                    Kind = table.Column<int>(
                        type: "int",
                        nullable: false),

                    SourceSignatureId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: true),

                    SourceStampId = table.Column<Guid>(
                        type: "uniqueidentifier",
                        nullable: true),

                    AppliedBlobPath = table.Column<string>(
                        type: "nvarchar(1000)",
                        maxLength: 1000,
                        nullable: false),

                    AppliedHash = table.Column<string>(
                        type: "nvarchar(128)",
                        maxLength: 128,
                        nullable: true),

                    AppliedAt = table.Column<DateTimeOffset>(
                        type: "datetimeoffset",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_EnvelopeAppliedSignatures",
                        x => x.Id);

                    table.CheckConstraint(
                        "CK_EnvelopeAppliedSignatures_Source",
                        """
                (
                    ([Kind] IN (1,2)
                        AND [SourceSignatureId] IS NOT NULL
                        AND [SourceStampId] IS NULL)
                    OR
                    ([Kind] = 3
                        AND [SourceStampId] IS NOT NULL
                        AND [SourceSignatureId] IS NULL)
                )
                """);

                    table.ForeignKey(
                        name: "FK_EnvelopeAppliedSignatures_Envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "Envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "FK_EnvelopeAppliedSignatures_EnvelopeFields_EnvelopeFieldId",
                        column: x => x.EnvelopeFieldId,
                        principalTable: "EnvelopeFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);

                    table.ForeignKey(
                        name: "FK_EnvelopeAppliedSignatures_EnvelopeRecipients_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "EnvelopeRecipients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "FK_EnvelopeAppliedSignatures_Users_AppliedByUserId",
                        column: x => x.AppliedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "FK_EnvelopeAppliedSignatures_Signatures_SourceSignatureId",
                        column: x => x.SourceSignatureId,
                        principalTable: "Signatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);

                    table.ForeignKey(
                        name: "FK_EnvelopeAppliedSignatures_Stamps_SourceStampId",
                        column: x => x.SourceStampId,
                        principalTable: "Stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
    name: "IX_EnvelopeAppliedSignatures_AppliedByUserId",
    table: "EnvelopeAppliedSignatures",
    column: "AppliedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeAppliedSignatures_EnvelopeFieldId",
                table: "EnvelopeAppliedSignatures",
                column: "EnvelopeFieldId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeAppliedSignatures_EnvelopeId",
                table: "EnvelopeAppliedSignatures",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeAppliedSignatures_RecipientId",
                table: "EnvelopeAppliedSignatures",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeAppliedSignatures_SourceSignatureId",
                table: "EnvelopeAppliedSignatures",
                column: "SourceSignatureId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvelopeAppliedSignatures_SourceStampId",
                table: "EnvelopeAppliedSignatures",
                column: "SourceStampId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvelopeAppliedSignatures");
        }
    }
}
