using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeriesPositionToDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "SeriesPosition",
                schema: "catalog",
                table: "Books",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 1,
                column: "SeriesPosition",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 2,
                column: "SeriesPosition",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SeriesPosition",
                schema: "catalog",
                table: "Books",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 1,
                column: "SeriesPosition",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 2,
                column: "SeriesPosition",
                value: null);
        }
    }
}
