using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;
using MyBooks.Common.Services;

namespace MyBooks.CatalogService.Controllers
{
    [Route("api/books")]
    [ApiController]
    //[Authorize]
    public class BookController :  ControllerBase
    {
        private readonly CatalogDbContext _context;
        private readonly IValidator<Book> _validator;
        private readonly HtmlSanitizationService _htmlSanitizationService;

        public BookController(
            CatalogDbContext context,
            HtmlSanitizationService htmlSanitizationService,
            IValidator<Book> validator)
        {
            _context = context;
            _htmlSanitizationService = htmlSanitizationService;
            _validator = validator;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            try
            {
                var books = await _context.Books
                    .Include(b => b.Genre)
                    .Include(b => b.AgeCategory)
                    .Include(b => b.Tags)
                    .Include(b => b.Series)
                    .ToListAsync();

                Console.WriteLine($"Fetched {books.Count} books.");
                return Ok(books);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.AgeCategory)
                .Include(b => b.Tags)
                .Include(b => b.Series)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound();

            return Ok(book);
        }

        //create new book
        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(Book book)
        {
            if (book == null) return BadRequest("Invalid book data.");

            var validationResult = await _validator.ValidateAsync(book);
            if (!validationResult.IsValid) return BadRequest($"Validation failed for new book: {book.Title}.");

            //sanitize all text fields, if they exist
            book.Title = _htmlSanitizationService.Sanitize(book.Title);
            if(!string.IsNullOrWhiteSpace(book.Author))
                book.Author = _htmlSanitizationService.Sanitize(book.Author);
            if(!string.IsNullOrWhiteSpace(book.Description))
                book.Description = _htmlSanitizationService.Sanitize(book.Description);
            if(!string.IsNullOrWhiteSpace(book.Location))
                book.Location = _htmlSanitizationService.Sanitize(book.Location);
            if (!string.IsNullOrWhiteSpace(book.TagInput))
            {
                var tagNames = book.TagInput.Split(',')
                    .Select(t => t.Trim().ToLowerInvariant())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .Select(t => _htmlSanitizationService.Sanitize(t))
                    .ToList();

                //disallow duplicates
                var existingTags = await _context.Tags
                    .Where(t => tagNames.Contains(t.Name.ToLower()))
                    .ToListAsync();

                var newTags = tagNames.Except(existingTags.Select(t => t.Name.ToLower()))
                    .Select(t => new Tag { Name = t })
                    .ToList();

                _context.Tags.AddRange(newTags);
                await _context.SaveChangesAsync();

                book.Tags = existingTags.Concat(newTags).ToList();
            }

        var genre = await _context.Genres.FindAsync(book.GenreId);
            if (genre == null) return BadRequest("Invalid genre ID.");
            book.Genre = genre;

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBook), new {id = book.Id}, book);
        }

        //update existing book
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Book book)
        {
            if (id != book.Id) return BadRequest("Book ID mismatch.");

            var existingBook = await _context.Books.Include(b => b.Genre).FirstOrDefaultAsync(b => b.Id == id);

            //verify authenticated user is either admin, editor, or creator

            //uncomment below once auth is added
            /*
            var userId = User.Identity.Name;
            var isAdmin = User.IsInRole("Admin");
            var isEditor = User.IsInRole("Editor");
            var isOwner = book.CreatedBy == userId;

            if (!isAdmin && !isEditor && !isOwner) return Forbid("Only an admin, editor, or the book's creator is authorized to update this book.");
            */

            //sanitize all text fields, if they exist
            book.Title = _htmlSanitizationService.Sanitize(book.Title);
            if (!string.IsNullOrWhiteSpace(book.Author))
                book.Author = _htmlSanitizationService.Sanitize(book.Author);
            if (!string.IsNullOrWhiteSpace(book.Description))
                book.Description = _htmlSanitizationService.Sanitize(book.Description);
            if (!string.IsNullOrWhiteSpace(book.Location))
                book.Location = _htmlSanitizationService.Sanitize(book.Location);
            if (!string.IsNullOrWhiteSpace(book.TagInput))
            {
                var tagNames = book.TagInput.Split(',')
                    .Select(t => t.Trim().ToLowerInvariant())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .Select(t => _htmlSanitizationService.Sanitize(t))
                    .ToList();

                //disallow duplicates
                var existingTags = await _context.Tags
                    .Where(t => tagNames.Contains(t.Name.ToLower()))
                    .ToListAsync();

                var newTags = tagNames.Except(existingTags.Select(t => t.Name.ToLower()))
                    .Select(t => new Tag { Name = t })
                    .ToList();

                _context.Tags.AddRange(newTags);
                await _context.SaveChangesAsync();

                book.Tags = existingTags.Concat(newTags).ToList();
            }

            existingBook.Title = book.Title;
            existingBook.Author = book.Author;
            existingBook.Description = book.Description;
            existingBook.Location = book.Location;
            existingBook.SeriesId = book.SeriesId;
            existingBook.TagInput = book.TagInput;
            existingBook.AgeCategoryId = book.AgeCategoryId;
            existingBook.PublishedDate = book.PublishedDate;
            existingBook.ISBN = book.ISBN;
            existingBook.GenreId = book.GenreId;

            _context.Entry(existingBook).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            //verify authenticated user is either admin, editor, or creator

            //uncomment below when auth is added
            /*
            var userId = User.Identity.Name;
            var isAdmin = User.IsInRole("Admin");
            var isEditor = User.IsInRole("Editor");
            var isOwner = book.CreatedBy == userId;

            if (!isAdmin && !isEditor && !isOwner) return Forbid("Only an admin, editor, or the book's creator is authorized to update this book.");
            */

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
