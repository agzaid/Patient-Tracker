using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newcolumnstoUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GeminiRequestsLastMinute",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GeminiRequestsToday",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGeminiRequestTime",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MedicationDocumentId",
                table: "Medications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiagnosisDocumentId",
                table: "Diagnoses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiagnosisDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtractionStatus = table.Column<int>(type: "int", nullable: false),
                    ExtractedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtractionError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawExtractionData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosisDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosisDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DiagnosisDocuments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtractionStatus = table.Column<int>(type: "int", nullable: false),
                    ExtractedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtractionError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawExtractionData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicationDocuments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medications_MedicationDocumentId",
                table: "Medications",
                column: "MedicationDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_DiagnosisDocumentId",
                table: "Diagnoses",
                column: "DiagnosisDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisDocuments_DocumentId",
                table: "DiagnosisDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosisDocuments_UserId",
                table: "DiagnosisDocuments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDocuments_DocumentId",
                table: "MedicationDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationDocuments_UserId",
                table: "MedicationDocuments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Diagnoses_DiagnosisDocuments_DiagnosisDocumentId",
                table: "Diagnoses",
                column: "DiagnosisDocumentId",
                principalTable: "DiagnosisDocuments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Medications_MedicationDocuments_MedicationDocumentId",
                table: "Medications",
                column: "MedicationDocumentId",
                principalTable: "MedicationDocuments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diagnoses_DiagnosisDocuments_DiagnosisDocumentId",
                table: "Diagnoses");

            migrationBuilder.DropForeignKey(
                name: "FK_Medications_MedicationDocuments_MedicationDocumentId",
                table: "Medications");

            migrationBuilder.DropTable(
                name: "DiagnosisDocuments");

            migrationBuilder.DropTable(
                name: "MedicationDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Medications_MedicationDocumentId",
                table: "Medications");

            migrationBuilder.DropIndex(
                name: "IX_Diagnoses_DiagnosisDocumentId",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "GeminiRequestsLastMinute",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GeminiRequestsToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastGeminiRequestTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MedicationDocumentId",
                table: "Medications");

            migrationBuilder.DropColumn(
                name: "DiagnosisDocumentId",
                table: "Diagnoses");
        }
    }
}
