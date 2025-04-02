using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.AuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddAcceptableUseRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptedAup",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAcceptedAup",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAup",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastAcceptedAup",
                table: "Users");
        }
    }
}
