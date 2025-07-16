using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeInventory.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRepositoryAnalysisToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnalysisDate",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Headline",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "HeroImage",
                table: "Projects",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepomixOutput",
                table: "Projects",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Headline",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "HeroImage",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RepomixOutput",
                table: "Projects");
        }
    }
}
