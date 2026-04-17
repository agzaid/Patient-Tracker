using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLanguagePreferenceFromUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LanguagePreference",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LanguagePreference",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }
    }
}
