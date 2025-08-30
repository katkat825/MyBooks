using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyBooks.CatalogService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgeCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgeCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
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
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GenreId = table.Column<int>(type: "int", nullable: false),
                    AgeCategoryId = table.Column<int>(type: "int", nullable: false),
                    TagInput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Books_AgeCategories_AgeCategoryId",
                        column: x => x.AgeCategoryId,
                        principalTable: "AgeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Books_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Books_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BookTag",
                columns: table => new
                {
                    BooksId = table.Column<int>(type: "int", nullable: false),
                    TagsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookTag", x => new { x.BooksId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_BookTag_Books_BooksId",
                        column: x => x.BooksId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AgeCategories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Children" },
                    { 2, "Young Adult" },
                    { 3, "Adult" }
                });

            migrationBuilder.InsertData(
                table: "Genres",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "LastModifiedBy", "LastModifiedDate", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Science Fiction", null },
                    { 2, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Fantasy", null },
                    { 3, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Mystery", null },
                    { 4, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Romance", null },
                    { 5, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Horror", null }
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "LastModifiedBy", "LastModifiedDate", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "spicy", 0 },
                    { 2, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "magic", 0 },
                    { 3, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "detective", 0 },
                    { 4, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "love", 0 },
                    { 5, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "monsters", 0 }
                });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "Id", "AgeCategoryId", "Author", "CreatedBy", "CreatedDate", "Description", "FileId", "GenreId", "ISBN", "LastModifiedBy", "LastModifiedDate", "Location", "PublishedDate", "SeriesId", "SeriesPosition", "TagInput", "Title" },
                values: new object[,]
                {
                    { 1, 3, "Frank Herbert", "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "A science fiction novel set in a distant future amidst a huge interstellar empire, where a young nobleman becomes embroiled in a complex struggle for control of the desert planet Arrakis.", null, 1, null, null, null, null, null, null, null, null, "Dune" },
                    { 2, 1, "J.R.R. Tolkien", "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "A fantasy novel that follows the journey of Bilbo Baggins, a hobbit who is swept into an epic quest to reclaim a treasure guarded by the dragon Smaug.", null, 2, null, null, null, null, null, null, null, null, "The Hobbit" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_AgeCategoryId",
                table: "Books",
                column: "AgeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_GenreId",
                table: "Books",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_SeriesId",
                table: "Books",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_BookTag_TagsId",
                table: "BookTag",
                column: "TagsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookTag");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "AgeCategories");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Series");
        }
    }
}
