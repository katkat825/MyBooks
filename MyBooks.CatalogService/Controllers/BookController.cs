using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;
using MyBooks.Common.Services;
using MyBooks.Common.BaseClasses;
using MyBooks.CatalogService.Services;
using MyBooks.Common.Helpers;
using MyBooks.Common.Dtos;

namespace MyBooks.CatalogService.Controllers;

[Route("api/books")]
[ApiController]
[Authorize]
public class BookController : ControllerBase
{
    private readonly CatalogDbContext _context;
    private readonly IValidator<Book> _validator;
    private readonly HtmlSanitizationService _htmlSanitizationService;
    private readonly HttpClient _httpClient;
    private readonly OpenLibraryClient _openLibraryClient;
    private readonly string _fileServiceBaseUrl;
    private readonly string _authServiceBaseUrl;
    private readonly string _systemTokenSecret;
    private readonly string serviceName = "CatalogService";

    public BookController(
        CatalogDbContext context,
        HtmlSanitizationService htmlSanitizationService,
        IValidator<Book> validator,
        IHttpClientFactory httpClientFactory,
        OpenLibraryClient openLibraryClient,
        IConfiguration config)
    {
        _context = context;
        _htmlSanitizationService = htmlSanitizationService;
        _validator = validator;
        _httpClient = httpClientFactory.CreateClient();
        _openLibraryClient = openLibraryClient;
        _fileServiceBaseUrl = config["BaseUrls:FileService"] ?? throw new ArgumentNullException("FileService base URL is not configured.");
        _authServiceBaseUrl = config["BaseUrls:AuthService"] ?? throw new ArgumentNullException("AuthService base URL is not configured.");
        _systemTokenSecret = config["ServiceSecrets:CatalogService"] ?? throw new ArithmeticException("CatalogService secret is not configured.");

    }

    // get recent reads
    [HttpGet("recently-read")]
    public async Task<IActionResult> GetRecentlyRead([FromQuery] int count = 10)
    {
        try
        {
            var tenantId = _context.GetCurrentTenantId();
            var userIdString = _context.GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(userIdString) || !int.TryParse(userIdString, out int userId))
                return Unauthorized("User not identified.");

            var tokenHelper = new SystemTokenHelper(_httpClient, _authServiceBaseUrl);
            var systemToken = await tokenHelper.GetSystemTokenAsync(serviceName, _systemTokenSecret);

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", systemToken);
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_fileServiceBaseUrl}/api/files/progress/recent?userId={userId}&count={count}");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, "Unable to retrieve reading progress data.");

            var progressList = await response.Content.ReadFromJsonAsync<List<ReadingProgressDto>>();

            Console.WriteLine($"Progress list raw content: {await response.Content.ReadAsStringAsync()}");
            Console.WriteLine($"Deserialized count: {(progressList == null ? "null" : progressList.Count.ToString())}");

            if (progressList == null || progressList.Count == 0)
                return Ok(new List<RecentlyReadDto>());
            var fileIds = progressList.Select(p => p.FileId).Distinct().ToList();

            var books = await _context.Books
                .Where(b => b.FileId.HasValue && fileIds.Contains(b.FileId.Value))
                .Include(b => b.Genre)
                .Include(b => b.AgeCategory)
                .Include(b => b.Tags)
                .Include(b => b.Series)
                .ToListAsync();

            var results = progressList
                .Join(books,
                    p => p.FileId,
                    b => b.FileId,
                    (p, b) => new RecentlyReadDto
                    {
                        BookId = b.Id,
                        Title = b.Title,
                        Author = b.Author,
                        Genre = b.Genre?.Name,
                        Series = b.Series?.Name,
                        ProgressPercent = p.ProgressPercent,
                        LastUpdated = p.LastUpdated,
                        FileId = b.FileId
                    })
                .OrderByDescending(x => x.LastUpdated)
                .ToList();

            Console.WriteLine($"Returning {results.Count} recently read books.");

            return Ok(results);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching recently read books: {ex.Message}");
            return StatusCode(500, "An error occurred while retrieving recently read books.");
        }
    }

    // get all books
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Book>>> GetBooks([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var ageCategoryClaim = User.FindFirst("AgeCategoryId")?.Value;
            if (string.IsNullOrWhiteSpace(ageCategoryClaim))
                return Unauthorized("User age category could not be determined.");

            int userAgeCategory = int.Parse(ageCategoryClaim);
            int tenantId = _context.GetCurrentTenantId();

            var query = _context.Books
                .IgnoreQueryFilters()
                .Where(b => b.TenantId == tenantId && b.IsActive && b.AgeCategoryId <= userAgeCategory)
                .Include(b => b.Genre)
                .Include(b => b.AgeCategory)
                .Include(b => b.Tags)
                .Include(b => b.Series)
                .OrderBy(b => b.Title);

            var total = await query.CountAsync();
            var books = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                Results = books
            });
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

        // sanitize all user-provided text fields, if they exist
        book.Title = _htmlSanitizationService.Sanitize(book.Title);
        if (!string.IsNullOrWhiteSpace(book.Author))
            book.Author = _htmlSanitizationService.Sanitize(book.Author);
        if (!string.IsNullOrWhiteSpace(book.Description))
            book.Description = _htmlSanitizationService.Sanitize(book.Description);
        if (!string.IsNullOrWhiteSpace(book.Location))
            book.Location = _htmlSanitizationService.Sanitize(book.Location);
        if (!string.IsNullOrWhiteSpace(book.ISBN) && !IsbnHelper.IsPlausibleIsbn(book.ISBN))
            book.ISBN = null;

        var preferredAuthors = _context.Books
            .Where(b => b.TenantId == _context.GetCurrentTenantId() && !string.IsNullOrEmpty(b.Author))
            .Select(b => b.Author)
            .Distinct()
            .ToList();

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

        // tags - not used yet
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
            var response = await _httpClient.GetAsync($"{_fileServiceBaseUrl}/api/files/{book.FileId}");
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
        if (!string.IsNullOrWhiteSpace(book.ISBN) && !IsbnHelper.IsPlausibleIsbn(book.ISBN))
            book.ISBN = null;
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
            var response = await _httpClient.GetAsync($"{_fileServiceBaseUrl}/api/files/{book.FileId}");
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
