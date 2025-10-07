using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;

namespace MyBooks.CatalogService.Controllers;

[ApiController]
[Route("api/support/[controller]")]
[Authorize(Roles = AppRoles.AllBooksAccess)]
public class SupportBookController : ControllerBase
{
    private readonly CatalogDbContext _context;

    public SupportBookController(CatalogDbContext context)
    {
        _context = context;
    }

    // get all books (active + inactive + restricted, across tenants)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Book>>> GetAllBooks()
    {
        var books = await _context.Books
            .IgnoreQueryFilters()
            .Include(b => b.Genre)
            .Include(b => b.AgeCategory)
            .Include(b => b.Tags)
            .Include(b => b.Series)
            .AsNoTracking()
            .ToListAsync();

        return Ok(books);
    }

    // get single book by id (ignores tenant/active filters)
    [HttpGet("{id}")]
    public async Task<ActionResult<Book>> GetBookById(int id)
    {
        var book = await _context.Books
            .IgnoreQueryFilters()
            .Include(b => b.Genre)
            .Include(b => b.AgeCategory)
            .Include(b => b.Tags)
            .Include(b => b.Series)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null)
            return NotFound();

        return Ok(book);
    }

    // update file reference for a book (used after FileService flips IsActive)
    [HttpPatch("{id}/file")]
    [Authorize(Roles = AppRoles.FileService)]
    public async Task<IActionResult> UpdateBookFileId(int id, [FromBody] BookFileLinkDto dto)
    {
        if (id != dto.BookId)
            return BadRequest("Book ID mismatch.");

        var book = await _context.Books
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null)
            return NotFound("Book not found.");

        book.FileId = dto.FileId;
        _context.Entry(book).State = EntityState.Modified;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // toggle IsRestricted (superadmin-only)
    [HttpPatch("{id}/restricted")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> ToggleRestricted(int id, [FromQuery] bool restricted)
    {
        var book = await _context.Books
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null)
            return NotFound("Book not found.");

        book.IsRestricted = restricted;
        _context.Entry(book).State = EntityState.Modified;

        await _context.SaveFlipRestrictedAsync();
        return NoContent();
    }
}
