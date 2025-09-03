using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.FileService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitCentralDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "file");

            migrationBuilder.CreateTable(
                name: "FilesMetaData",
                schema: "file",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UploadedByIp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilesMetaData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadingProgress",
                schema: "file",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<double>(type: "float", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingProgress", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilesMetaData_BookId",
                schema: "file",
                table: "FilesMetaData",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgress_FileId_UserId",
                schema: "file",
                table: "ReadingProgress",
                columns: new[] { "FileId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilesMetaData",
                schema: "file");

            migrationBuilder.DropTable(
                name: "ReadingProgress",
                schema: "file");
        }
    }
}
