using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.TenantService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxUserCountToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxUserCount",
                schema: "tenant",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxUserCount",
                schema: "tenant",
                table: "Tenants");
        }
    }
}
