using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.FileService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleDriveIntegrationsFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GoogleIntegrationId",
                schema: "file",
                table: "FilesMetaData",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GoogleIntegrations",
                schema: "file",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AccountEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DriveFolderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleIntegrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilesMetaData_GoogleIntegrationId",
                schema: "file",
                table: "FilesMetaData",
                column: "GoogleIntegrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FilesMetaData_GoogleIntegrations_GoogleIntegrationId",
                schema: "file",
                table: "FilesMetaData",
                column: "GoogleIntegrationId",
                principalSchema: "file",
                principalTable: "GoogleIntegrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FilesMetaData_GoogleIntegrations_GoogleIntegrationId",
                schema: "file",
                table: "FilesMetaData");

            migrationBuilder.DropTable(
                name: "GoogleIntegrations",
                schema: "file");

            migrationBuilder.DropIndex(
                name: "IX_FilesMetaData_GoogleIntegrationId",
                schema: "file",
                table: "FilesMetaData");

            migrationBuilder.DropColumn(
                name: "GoogleIntegrationId",
                schema: "file",
                table: "FilesMetaData");
        }
    }
}
