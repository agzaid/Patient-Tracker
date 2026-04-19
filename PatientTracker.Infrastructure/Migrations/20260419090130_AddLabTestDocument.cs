using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLabTestDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LabTestDocumentId",
                table: "LabTests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabTestDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_LabTestDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabTestDocuments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabTests_LabTestDocumentId",
                table: "LabTests",
                column: "LabTestDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTestDocuments_UserId",
                table: "LabTestDocuments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LabTests_LabTestDocuments_LabTestDocumentId",
                table: "LabTests",
                column: "LabTestDocumentId",
                principalTable: "LabTestDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabTests_LabTestDocuments_LabTestDocumentId",
                table: "LabTests");

            migrationBuilder.DropTable(
                name: "LabTestDocuments");

            migrationBuilder.DropIndex(
                name: "IX_LabTests_LabTestDocumentId",
                table: "LabTests");

            migrationBuilder.DropColumn(
                name: "LabTestDocumentId",
                table: "LabTests");
        }
    }
}
