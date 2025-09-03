using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyBooks.TenantService.Migrations
{
    /// <inheritdoc />
    public partial class FurtherSimplified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "BillingPlans",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "AllowExternalIntegrations",
                table: "BillingPlans");

            migrationBuilder.DropColumn(
                name: "AllowStorage",
                table: "BillingPlans");

            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "BillingPlans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowExternalIntegrations",
                table: "BillingPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowStorage",
                table: "BillingPlans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "BillingPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "BillingPlans",
                columns: new[] { "Id", "AllowExternalIntegrations", "AllowStorage", "AnnualPrice", "CreatedBy", "CreatedDate", "IsActive", "LastModifiedBy", "LastModifiedDate", "MaxStorageMb", "MaxUsers", "MonthlyPrice", "Name" },
                values: new object[] { 1, true, true, 0m, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, 0, 0, 0m, "Dev Testing" });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "BillingPlanId", "CreatedBy", "CreatedDate", "IsActive", "LastModifiedBy", "LastModifiedDate", "Name", "OwnerUserId", "Subdomain" },
                values: new object[,]
                {
                    { 1, 1, "System", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Tenant One", 3, "tenant1" },
                    { 2, 1, "System", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Tenant Two", 4, "tenant2" }
                });
        }
    }
}
