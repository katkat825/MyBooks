using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.SupportService.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                schema: "support",
                table: "ReportLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                schema: "support",
                table: "ReportLogs");
        }
    }
}
