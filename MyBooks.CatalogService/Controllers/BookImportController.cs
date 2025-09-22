using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;
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

        public BookImportController(
            CatalogDbContext context,
            HtmlSanitizationService htmlSanitizationService)
        {
            _context = context;
            _htmlSanitizationService = htmlSanitizationService;
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
            var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == dto.BookId);
            if (book == null) return NotFound();

            book.FileId = dto.FileId;
            book.IsActive = true;

            await _context.SaveChangesAsSystemAsync();
            return NoContent();
        }
    }
}
