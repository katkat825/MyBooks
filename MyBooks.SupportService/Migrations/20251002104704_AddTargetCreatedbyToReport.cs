using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.SupportService.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetCreatedbyToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TargetCreatedBy",
                schema: "support",
                table: "ReportLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetCreatedBy",
                schema: "support",
                table: "ReportLogs");
        }
    }
}
