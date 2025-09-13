using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.FileService.Data.Migrations
{
    /// <inheritdoc />
    public partial class GoogleFolderIdInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DriveFolderId",
                schema: "file",
                table: "GoogleIntegrations",
                newName: "DriveFolderIds");

            migrationBuilder.AddColumn<string>(
                name: "FolderId",
                schema: "file",
                table: "FilesMetaData",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FolderId",
                schema: "file",
                table: "FilesMetaData");

            migrationBuilder.RenameColumn(
                name: "DriveFolderIds",
                schema: "file",
                table: "GoogleIntegrations",
                newName: "DriveFolderId");
        }
    }
}
