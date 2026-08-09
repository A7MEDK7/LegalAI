using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalOpinionIdToLegalDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LegalDocuments_LegalOpinions_LegalOpinionId",
                table: "LegalDocuments");

            migrationBuilder.AddForeignKey(
                name: "FK_LegalDocuments_LegalOpinions_LegalOpinionId",
                table: "LegalDocuments",
                column: "LegalOpinionId",
                principalTable: "LegalOpinions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LegalDocuments_LegalOpinions_LegalOpinionId",
                table: "LegalDocuments");

            migrationBuilder.AddForeignKey(
                name: "FK_LegalDocuments_LegalOpinions_LegalOpinionId",
                table: "LegalDocuments",
                column: "LegalOpinionId",
                principalTable: "LegalOpinions",
                principalColumn: "Id");
        }
    }
}
