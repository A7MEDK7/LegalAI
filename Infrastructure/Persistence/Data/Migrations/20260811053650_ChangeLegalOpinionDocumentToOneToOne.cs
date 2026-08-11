using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeLegalOpinionDocumentToOneToOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LegalDocuments_LegalOpinionId",
                table: "LegalDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_LegalOpinionId",
                table: "LegalDocuments",
                column: "LegalOpinionId",
                unique: true,
                filter: "[LegalOpinionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LegalDocuments_LegalOpinionId",
                table: "LegalDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_LegalOpinionId",
                table: "LegalDocuments",
                column: "LegalOpinionId");
        }
    }
}
