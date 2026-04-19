using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentIdToLabTestDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DocumentId",
                table: "LabTestDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabTestDocuments_DocumentId",
                table: "LabTestDocuments",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_LabTestDocuments_Documents_DocumentId",
                table: "LabTestDocuments",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabTestDocuments_Documents_DocumentId",
                table: "LabTestDocuments");

            migrationBuilder.DropIndex(
                name: "IX_LabTestDocuments_DocumentId",
                table: "LabTestDocuments");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "LabTestDocuments");
        }
    }
}
