using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStorageProviderToBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalFileId",
                schema: "catalog",
                table: "Books",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntegrationId",
                schema: "catalog",
                table: "Books",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                schema: "catalog",
                table: "Books",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ExternalFileId", "IntegrationId", "Provider" },
                values: new object[] { null, null, 0 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ExternalFileId", "IntegrationId", "Provider" },
                values: new object[] { null, null, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalFileId",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "IntegrationId",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Provider",
                schema: "catalog",
                table: "Books");
        }
    }
}
