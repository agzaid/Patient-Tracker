using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class nationalityColumnAddedToModelProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "Profiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "Profiles");
        }
    }
}
