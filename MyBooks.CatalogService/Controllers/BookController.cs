using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;
using MyBooks.Common.Services;
using MyBooks.Common.BaseClasses;

namespace MyBooks.CatalogService.Controllers
{
    [Route("api/books")]
    [ApiController]
    [Authorize]
    public class BookController :  ControllerBase
    {
        private readonly CatalogDbContext _context;
        private readonly IValidator<Book> _validator;
        private readonly HtmlSanitizationService _htmlSanitizationService;
        private readonly HttpClient _httpClient;

        public BookController(
            CatalogDbContext context,
            HtmlSanitizationService htmlSanitizationService,
            IValidator<Book> validator,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _htmlSanitizationService = htmlSanitizationService;
            _validator = validator;
            _httpClient = httpClientFactory.CreateClient();
        }

        // get all books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            try
            {
                var ageCategoryClaim = User.FindFirst("AgeCategoryId")?.Value;
                if (string.IsNullOrWhiteSpace(ageCategoryClaim)) return Unauthorized("User age category could not be determined.");
                int userAgeCategory = int.Parse(ageCategoryClaim);

                var books = await _context.Books
                    .Where(b => b.AgeCategoryId <= userAgeCategory)
                    .Include(b => b.Genre)
                    .Include(b => b.AgeCategory)
                    .Include(b => b.Tags)
                    .Include(b => b.Series)
                    .ToListAsync();

                Console.WriteLine($"Fetched {books.Count} books. User AgeCategoryId {userAgeCategory}");
                return books;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500);
            }
        }

        // get 1 book
        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            var ageCategoryClaim = User.FindFirst("AgeCategoryId")?.Value;
            if (string.IsNullOrWhiteSpace(ageCategoryClaim)) return Unauthorized("User age category cannot be determined.");
            int userAgeCategory = int.Parse(ageCategoryClaim);

            var book = await _context.Books
                .Where(b => b.Id == id && b.AgeCategoryId <= userAgeCategory)
                .Include(b => b.Genre)
                .Include(b => b.AgeCategory)
                .Include(b => b.Tags)
                .Include(b => b.Series)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound();

            return Ok(book);
        }

        //create new book - owner & superadmin only
        [HttpPost]
        [Authorize(Roles = AppRoles.OwnerPlus)]
        public async Task<ActionResult<Book>> PostBook(Book book)
        {
            if (book == null) return BadRequest("Invalid book data.");

            var validationResult = await _validator.ValidateAsync(book);
            if (!validationResult.IsValid) return BadRequest($"Validation failed for new book: {book.Title}.");

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

            var genre = await _context.Genres.FindAsync(book.GenreId);
            if (genre == null) return BadRequest("Invalid genre ID.");
            book.Genre = genre;

            if (book.FileId.HasValue)
            {
                var response = await _httpClient.GetAsync($"https://localhost:7142/api/files/{book.FileId}");
                if (!response.IsSuccessStatusCode) return BadRequest("Invalid FileId. File not found.");
            }

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }

        //update existing book - owner & superadmin only
        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.OwnerPlus)]
        public async Task<IActionResult> PutBook(int id, Book book)
        {
            if (id != book.Id) return BadRequest("Book ID mismatch.");

            var existingBook = await _context.Books.Include(b => b.Genre).FirstOrDefaultAsync(b => b.Id == id);

            if (existingBook == null) return NotFound();

            if (existingBook.IsRestricted)
                return Forbid($"Book '{existingBook.Title}' is restricted and cannot be modified.");

            //verify authenticated user is either admin, editor, or creator
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var canEdit = AppRoles.EditorsArray.Any(User.IsInRole);
            var isCreator = book.CreatedBy == userId.ToString();

            if (!canEdit && !isCreator) return Forbid("Only an admin, editor, or the book's creator is authorized to update this book.");

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

            if (book.FileId.HasValue)
            {
                var response = await _httpClient.GetAsync($"https://localhost:7142/api/files/{book.FileId}");
                if (!response.IsSuccessStatusCode) return BadRequest("Invalid FileId. File not found.");
                existingBook.FileId = book.FileId;
            }

            existingBook.Title = book.Title;
            existingBook.Author = book.Author;
            existingBook.Description = book.Description;
            existingBook.Location = book.Location;
            existingBook.SeriesId = book.SeriesId;
            existingBook.SeriesPosition = book.SeriesPosition;
            existingBook.TagInput = book.TagInput;
            existingBook.AgeCategoryId = book.AgeCategoryId;
            existingBook.PublishedDate = book.PublishedDate;
            existingBook.ISBN = book.ISBN;
            existingBook.GenreId = book.GenreId;


            _context.Entry(existingBook).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{id}/file")]
        [Authorize(Roles = AppRoles.OwnerPlus)]
        public async Task<IActionResult> UpdateBookFileId(int id, [FromBody] FileUpdateDto request)
        {
            Console.WriteLine($"📡 Received request to update book {id} with FileId: {request.FileId}");

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound("Book not found.");
            }

            if (book.IsRestricted)
                return Forbid($"Book '{book.Title}' is restricted and its file cannot be changed.");

            if (request.FileId <= 0)
            {
                return BadRequest("Invalid FileId.");
            }

            book.FileId = request.FileId;
            _context.Entry(book).State = EntityState.Modified;

            await _context.SaveChangesAsync();


            return NoContent();
        }

        public class FileUpdateDto
        {
            public int FileId { get; set; }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.OwnerPlus)]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            if (book.IsRestricted)
                return Forbid($"Book '{book.Title}' is restricted and cannot be deleted.");

            //verify authenticated user is either admin, editor, or creator
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var canEdit = AppRoles.EditorsArray.Any(User.IsInRole);
            var isCreator = book.CreatedBy == userId.ToString();

            if (!canEdit && !isCreator) return Forbid("Only an admin, editor, or the book's creator is authorized to delete this book.");

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
