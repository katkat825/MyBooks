using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.SupportService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateImpersonationLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "support",
                table: "ImpersonationLogs");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                schema: "support",
                table: "ImpersonationLogs");

            migrationBuilder.RenameColumn(
                name: "LastModifiedDate",
                schema: "support",
                table: "ImpersonationLogs",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                schema: "support",
                table: "ImpersonationLogs",
                newName: "StartTime");

            migrationBuilder.AddColumn<int>(
                name: "ImpersonatingUserId",
                schema: "support",
                table: "ImpersonationLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImpersonatingUserId",
                schema: "support",
                table: "ImpersonationLogs");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                schema: "support",
                table: "ImpersonationLogs",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                schema: "support",
                table: "ImpersonationLogs",
                newName: "LastModifiedDate");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "support",
                table: "ImpersonationLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                schema: "support",
                table: "ImpersonationLogs",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
