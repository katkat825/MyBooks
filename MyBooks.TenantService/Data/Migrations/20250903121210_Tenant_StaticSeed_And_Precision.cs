using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.TenantService.Data.Migrations
{
    /// <inheritdoc />
    public partial class Tenant_StaticSeed_And_Precision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercent",
                schema: "tenant",
                table: "Tenants",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.InsertData(
                schema: "tenant",
                table: "Tenants",
                columns: new[] { "Id", "BillingPlanId", "CreatedBy", "CreatedDate", "CreditBalance", "DiscountPercent", "IsActive", "LastModifiedBy", "LastModifiedDate", "Name", "OwnerUserId", "Subdomain" },
                values: new object[] { 1, 1, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, null, true, null, null, "Dev Tenant", 1, "dev" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "tenant",
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountPercent",
                schema: "tenant",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);
        }
    }
}
