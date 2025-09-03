using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyBooks.CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitCentralDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "AgeCategories",
                schema: "catalog",
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
                schema: "catalog",
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
                schema: "catalog",
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
                name: "Books",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsRestricted = table.Column<bool>(type: "bit", nullable: false),
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
                    MasterBookId = table.Column<int>(type: "int", nullable: true),
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
                        principalSchema: "catalog",
                        principalTable: "AgeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Books_Genres_GenreId",
                        column: x => x.GenreId,
                        principalSchema: "catalog",
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Books_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalSchema: "catalog",
                        principalTable: "Series",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MasterBooks",
                schema: "catalog",
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

            migrationBuilder.CreateTable(
                name: "Tags",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MasterBookId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_MasterBooks_MasterBookId",
                        column: x => x.MasterBookId,
                        principalSchema: "catalog",
                        principalTable: "MasterBooks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BookTag",
                schema: "catalog",
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
                        principalSchema: "catalog",
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalSchema: "catalog",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "AgeCategories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Children" },
                    { 2, "Young Adult" },
                    { 3, "Adult" }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Genres",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "LastModifiedBy", "LastModifiedDate", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Science Fiction", 1 },
                    { 2, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Fantasy", 1 },
                    { 3, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Mystery", 1 },
                    { 4, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Romance", 1 },
                    { 5, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, "Horror", 1 }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Tags",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "IsActive", "LastModifiedBy", "LastModifiedDate", "MasterBookId", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, null, "spicy", 1 },
                    { 2, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, null, "magic", 1 },
                    { 3, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, null, "detective", 1 },
                    { 4, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, null, "love", 1 },
                    { 5, "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), true, null, null, null, "monsters", 1 }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Books",
                columns: new[] { "Id", "AgeCategoryId", "Author", "CreatedBy", "CreatedDate", "Description", "FileId", "GenreId", "ISBN", "IsActive", "IsRestricted", "LastModifiedBy", "LastModifiedDate", "Location", "MasterBookId", "PublishedDate", "SeriesId", "SeriesPosition", "TagInput", "TenantId", "Title" },
                values: new object[,]
                {
                    { 1, 3, "Frank Herbert", "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "A science fiction novel set in a distant future amidst a huge interstellar empire, where a young nobleman becomes embroiled in a complex struggle for control of the desert planet Arrakis.", null, 1, null, true, false, null, null, null, null, null, null, null, null, 1, "Dune" },
                    { 2, 1, "J.R.R. Tolkien", "system", new DateTime(2025, 2, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "A fantasy novel that follows the journey of Bilbo Baggins, a hobbit who is swept into an epic quest to reclaim a treasure guarded by the dragon Smaug.", null, 2, null, true, false, null, null, null, null, null, null, null, null, 1, "The Hobbit" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_AgeCategoryId",
                schema: "catalog",
                table: "Books",
                column: "AgeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_GenreId",
                schema: "catalog",
                table: "Books",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_SeriesId",
                schema: "catalog",
                table: "Books",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_BookTag_TagsId",
                schema: "catalog",
                table: "BookTag",
                column: "TagsId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Tags_MasterBookId",
                schema: "catalog",
                table: "Tags",
                column: "MasterBookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookTag",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Books",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Tags",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "MasterBooks",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "AgeCategories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Genres",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Series",
                schema: "catalog");
        }
    }
}
