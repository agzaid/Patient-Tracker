using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatientTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeminiUsageTrackingToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GeminiRequestsToday",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GeminiRequestsLastMinute",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGeminiRequestTime",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeminiRequestsToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GeminiRequestsLastMinute",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastGeminiRequestTime",
                table: "Users");
        }
    }
}
