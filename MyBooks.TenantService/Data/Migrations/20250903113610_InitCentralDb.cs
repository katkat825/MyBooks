using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyBooks.TenantService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitCentralDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenant");

            migrationBuilder.CreateTable(
                name: "BillingPlans",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxStorageMb = table.Column<int>(type: "int", nullable: false),
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
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BillingPlanId = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreditBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
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
                        principalSchema: "tenant",
                        principalTable: "BillingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "tenant",
                table: "BillingPlans",
                columns: new[] { "Id", "AnnualPrice", "CreatedBy", "CreatedDate", "IsActive", "LastModifiedBy", "LastModifiedDate", "MaxStorageMb", "MonthlyPrice", "Name" },
                values: new object[,]
                {
                    { 1, 0m, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, 1024, 0m, "Free" },
                    { 2, 40m, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, 5120, 4m, "Basic" },
                    { 3, 80m, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, 15360, 8m, "Standard" },
                    { 4, 150m, "System", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, 51200, 15m, "Premium" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_BillingPlanId",
                schema: "tenant",
                table: "Tenants",
                column: "BillingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                schema: "tenant",
                table: "Tenants",
                column: "Subdomain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "BillingPlans",
                schema: "tenant");
        }
    }
}
