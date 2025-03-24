using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.FileService.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Files",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgresses_FileId_UserId",
                table: "ReadingProgresses",
                columns: new[] { "FileId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReadingProgresses_FileId_UserId",
                table: "ReadingProgresses");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Files");
        }
    }
}
