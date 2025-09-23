using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.FileService.Data.Migrations
{
    /// <inheritdoc />
    public partial class IntegrationIdInBulkImportJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GoogleIntegrationId",
                schema: "file",
                table: "BulkImportJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AgeCategoryId",
                schema: "file",
                table: "BulkImportItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GenreId",
                schema: "file",
                table: "BulkImportItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                schema: "file",
                table: "BulkImportItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleIntegrationId",
                schema: "file",
                table: "BulkImportJobs");

            migrationBuilder.DropColumn(
                name: "AgeCategoryId",
                schema: "file",
                table: "BulkImportItems");

            migrationBuilder.DropColumn(
                name: "GenreId",
                schema: "file",
                table: "BulkImportItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "file",
                table: "BulkImportItems");
        }
    }
}
