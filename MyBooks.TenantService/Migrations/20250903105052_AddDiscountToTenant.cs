using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyBooks.TenantService.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditBalance",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "BillingPlans",
                columns: new[] { "Id", "AnnualPrice", "CreatedBy", "CreatedDate", "IsActive", "LastModifiedBy", "LastModifiedDate", "MaxStorageMb", "MonthlyPrice", "Name" },
                values: new object[,]
                {
                    { 1, 0m, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, 1024, 0m, "Free" },
                    { 2, 40m, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, 5120, 4m, "Basic" },
                    { 3, 80m, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, 15360, 8m, "Standard" },
                    { 4, 150m, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, 51200, 15m, "Premium" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "CreditBalance",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Tenants");
        }
    }
}
