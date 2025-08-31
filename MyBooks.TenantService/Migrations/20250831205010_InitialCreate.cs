using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyBooks.TenantService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    MaxStorageMb = table.Column<int>(type: "int", nullable: false),
                    AllowStorage = table.Column<bool>(type: "bit", nullable: false),
                    AllowExternalIntegrations = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillingPlanId = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_BillingPlans_BillingPlanId",
                        column: x => x.BillingPlanId,
                        principalTable: "BillingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_BillingPlanId",
                table: "Tenants",
                column: "BillingPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "BillingPlans");
        }
    }
}
