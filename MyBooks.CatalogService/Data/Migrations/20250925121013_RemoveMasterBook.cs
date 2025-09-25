using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyBooks.CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMasterBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_MasterBooks_MasterBookId",
                schema: "catalog",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "MasterBooks",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_Tags_MasterBookId",
                schema: "catalog",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "MasterBookId",
                schema: "catalog",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "MasterBookId",
                schema: "catalog",
                table: "Books");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MasterBookId",
                schema: "catalog",
                table: "Tags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MasterBookId",
                schema: "catalog",
                table: "Books",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MasterBooks",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgeCategoryId = table.Column<int>(type: "int", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: false),
                    SeriesId = table.Column<int>(type: "int", nullable: true),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ISBN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SeriesPosition = table.Column<int>(type: "int", nullable: true),
                    TagInput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterBooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterBooks_AgeCategories_AgeCategoryId",
                        column: x => x.AgeCategoryId,
                        principalSchema: "catalog",
                        principalTable: "AgeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MasterBooks_Genres_GenreId",
                        column: x => x.GenreId,
                        principalSchema: "catalog",
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MasterBooks_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalSchema: "catalog",
                        principalTable: "Series",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 1,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Books",
                keyColumn: "Id",
                keyValue: 2,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Tags",
                keyColumn: "Id",
                keyValue: 1,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Tags",
                keyColumn: "Id",
                keyValue: 2,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Tags",
                keyColumn: "Id",
                keyValue: 3,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Tags",
                keyColumn: "Id",
                keyValue: 4,
                column: "MasterBookId",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Tags",
                keyColumn: "Id",
                keyValue: 5,
                column: "MasterBookId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_MasterBookId",
                schema: "catalog",
                table: "Tags",
                column: "MasterBookId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBooks_AgeCategoryId",
                schema: "catalog",
                table: "MasterBooks",
                column: "AgeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBooks_GenreId",
                schema: "catalog",
                table: "MasterBooks",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterBooks_SeriesId",
                schema: "catalog",
                table: "MasterBooks",
                column: "SeriesId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_MasterBooks_MasterBookId",
                schema: "catalog",
                table: "Tags",
                column: "MasterBookId",
                principalSchema: "catalog",
                principalTable: "MasterBooks",
                principalColumn: "Id");
        }
    }
}
