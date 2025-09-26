using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;
using MyBooks.CatalogService.Services;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using MyBooks.Common.Services;
using System.IO;

namespace MyBooks.CatalogService.Controllers
{
    [ApiController]
    [Route("api/book-import")]
    [Authorize(Roles = AppRoles.FileService)] 
    public class BookImportController : ControllerBase
    {
        private readonly CatalogDbContext _context;
        private readonly HtmlSanitizationService _htmlSanitizationService;
        private readonly OpenLibraryClient _openLibraryClient;

        public BookImportController(
            CatalogDbContext context,
            HtmlSanitizationService htmlSanitizationService,
            OpenLibraryClient openLibraryClient)
        {
            _context = context;
            _htmlSanitizationService = htmlSanitizationService;
            _openLibraryClient = openLibraryClient;
        }

        // Create a new Book during bulk import
        [HttpPost]
        public async Task<ActionResult<BookImportResponseDto>> ImportBook([FromBody] BookImportRequestDto dto)
        {
            if (dto == null) return BadRequest("Invalid request.");

            // Sanitize Title & Author
            var title = _htmlSanitizationService.Sanitize(dto.Title ?? string.Empty);
            if (string.IsNullOrWhiteSpace(title))
            {
                var safeName = _htmlSanitizationService.Sanitize(Path.GetFileNameWithoutExtension(dto.FileName));
                string fallback = "Untitled - bulk import " + DateTime.UtcNow.ToString("M/d/yyyy");
                title = string.IsNullOrWhiteSpace(safeName) ? fallback : safeName;
            }

            var author = string.IsNullOrWhiteSpace(dto.Author)
                ? null
                : _htmlSanitizationService.Sanitize(dto.Author);

            var book = new Book
            {
                Title = title,
                Author = author,
                GenreId = dto.GenreId,
                AgeCategoryId = dto.AgeCategoryId,
                TenantId = dto.TenantId,
                CreatedBy = "bulk import",
                CreatedDate = DateTime.UtcNow,
                IsActive = false
            };

            var preferredAuthors = await _context.Books
                .Where(b => b.TenantId == dto.TenantId && !string.IsNullOrEmpty(b.Author))
                .Select(b => b.Author)
                .Distinct()
                .ToListAsync();

            var lookupDto = new OpenLibraryLookupDto
            {
                Title = book.Title,
                PreferredAuthors = preferredAuthors
            };

            // enrich book via openlibraryclient
            OpenLibraryBookDto? metadata = null;
            if (!string.IsNullOrWhiteSpace(book.Title))
            {
                metadata = await _openLibraryClient.LookupByTitleAsync(lookupDto);
            }

            // fill in only missing fields from metadata
            if (metadata != null)
            {
                if (string.IsNullOrWhiteSpace(book.Author) && !string.IsNullOrWhiteSpace(metadata.Author))
                    book.Author = metadata.Author;

                if (!book.PublishedDate.HasValue && metadata.PublishedDate.HasValue)
                    book.PublishedDate = metadata.PublishedDate;

                if (string.IsNullOrWhiteSpace(book.ISBN) && !string.IsNullOrWhiteSpace(metadata.ISBN))
                    book.ISBN = metadata.ISBN;
            }


            _context.Books.Add(book);
            await _context.SaveChangesAsSystemAsync();

            return Ok(new BookImportResponseDto
            {
                BookId = book.Id,
                FilePath = dto.FilePath
            });
        }

        // Attach FileId after FileService has created the File row
        [HttpPatch("file")]
        public async Task<IActionResult> AttachFile([FromBody] BookFileLinkDto dto)
        {
            var book = await _context.Books
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Id == dto.BookId);
            if (book == null) return NotFound();

            book.FileId = dto.FileId;
            book.IsActive = true;

            await _context.SaveChangesAsSystemAsync();
            return NoContent();
        }
    }
}
