using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.FileService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileMetadataConversionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConvertedFilePath",
                schema: "file",
                table: "FilesMetaData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConverted",
                schema: "file",
                table: "FilesMetaData",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageSource",
                schema: "file",
                table: "FilesMetaData",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvertedFilePath",
                schema: "file",
                table: "FilesMetaData");

            migrationBuilder.DropColumn(
                name: "IsConverted",
                schema: "file",
                table: "FilesMetaData");

            migrationBuilder.DropColumn(
                name: "StorageSource",
                schema: "file",
                table: "FilesMetaData");
        }
    }
}
