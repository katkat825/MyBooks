using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.TenantService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSubdomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Subdomain",
                schema: "tenant",
                table: "Tenants");

            migrationBuilder.DeleteData(
                schema: "tenant",
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "tenant",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Subdomain",
                schema: "tenant",
                table: "Tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "tenant",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Subdomain",
                schema: "tenant",
                table: "Tenants",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                schema: "tenant",
                table: "Tenants",
                columns: new[] { "Id", "BillingPlanId", "CreatedBy", "CreatedDate", "CreditBalance", "DiscountPercent", "IsActive", "LastModifiedBy", "LastModifiedDate", "Name", "OwnerUserId", "Subdomain" },
                values: new object[] { 1, 1, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, null, true, null, null, "Dev Tenant", 1, "dev" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                schema: "tenant",
                table: "Tenants",
                column: "Subdomain",
                unique: true);
        }
    }
}
