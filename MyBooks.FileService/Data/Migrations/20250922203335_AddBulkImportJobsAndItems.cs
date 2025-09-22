using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.FileService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkImportJobsAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkImportJobs",
                schema: "file",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalFiles = table.Column<int>(type: "int", nullable: false),
                    ProcessedFiles = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BulkImportItems",
                schema: "file",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BulkImportJobId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBookId = table.Column<int>(type: "int", nullable: true),
                    CreatedFileId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkImportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkImportItems_BulkImportJobs_BulkImportJobId",
                        column: x => x.BulkImportJobId,
                        principalSchema: "file",
                        principalTable: "BulkImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkImportItems_BulkImportJobId",
                schema: "file",
                table: "BulkImportItems",
                column: "BulkImportJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkImportItems",
                schema: "file");

            migrationBuilder.DropTable(
                name: "BulkImportJobs",
                schema: "file");
        }
    }
}
