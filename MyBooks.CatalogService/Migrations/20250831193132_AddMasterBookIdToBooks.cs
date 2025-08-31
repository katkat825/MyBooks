using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.CatalogService.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterBookIdToBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MasterBookId",
                table: "Tags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MasterBookId",
                table: "Books",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MasterBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeriesId = table.Column<int>(type: "int", nullable: true),
                    SeriesPosition = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ISBN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GenreId = table.Column<int>(type: "int", nullable: false),
                    AgeCategoryId = table.Column<int>(type: "int", nullable: false),
                    TagInput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterBooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterBooks_AgeCategories_AgeCategoryId",
                        column: x => x.AgeCategoryId,
                        principalTable: "AgeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MasterBooks_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MasterBooks_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 1,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 2,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 3,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 4,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Tags",
                keyColumn: "Id",
                keyValue: 5,
                column: "MasterBookId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_MasterBookId",
                table: "Tags",
                column: "MasterBookId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBooks_AgeCategoryId",
                table: "MasterBooks",
                column: "AgeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBooks_GenreId",
                table: "MasterBooks",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBooks_SeriesId",
                table: "MasterBooks",
                column: "SeriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_MasterBooks_MasterBookId",
                table: "Tags",
                column: "MasterBookId",
                principalTable: "MasterBooks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_MasterBooks_MasterBookId",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "MasterBooks");

            migrationBuilder.DropIndex(
                name: "IX_Tags_MasterBookId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "MasterBookId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "MasterBookId",
                table: "Books");
        }
    }
}
