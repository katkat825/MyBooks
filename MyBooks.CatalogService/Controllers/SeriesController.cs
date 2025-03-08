using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;

namespace MyBooks.CatalogService.Controllers
{
    [Route("api/books/series")]
    [ApiController]
    //[Authorize]
    public class SeriesController : ControllerBase
    {
        private readonly CatalogDbContext _context;

        public SeriesController(CatalogDbContext context)
        {
            _context = context;
        }

        // 🔹 GET: Fetch all series
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Series>>> GetAllSeries()
        {
            return await _context.Series.ToListAsync();
        }

        // 🔹 GET: Fetch a single series by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Series>> GetSeries(int id)
        {
            var series = await _context.Series
                .Include(s => s.Books) // Include related books
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null)
                return NotFound("Series not found.");

            return Ok(series);
        }

        // 🔹 POST: Create a new series
        [HttpPost]
        public async Task<ActionResult<Series>> CreateSeries(Series series)
        {
            if (string.IsNullOrWhiteSpace(series.Name))
                return BadRequest("Series name is required.");

            series.CreatedBy = "System";  // Replace with real user later
            series.CreatedDate = DateTime.UtcNow;

            _context.Series.Add(series);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSeries), new { id = series.Id }, series);
        }

        // 🔹 PUT: Update an existing series
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSeries(int id, Series updatedSeries)
        {
            if (id != updatedSeries.Id)
                return BadRequest("Series ID mismatch.");

            var series = await _context.Series.FindAsync(id);
            if (series == null)
                return NotFound("Series not found.");

            series.Name = updatedSeries.Name;
            series.LastModifiedBy = "System"; // Replace with real user later
            series.LastModifiedDate = DateTime.UtcNow;

            _context.Entry(series).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 GET: Get all books in a series
        [HttpGet("{id}/books")]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooksInSeries(int id)
        {
            var books = await _context.Books
                .Where(b => b.SeriesId == id)
                .OrderBy(b => b.SeriesPosition)  // Ordered by book number in series
                .ToListAsync();

            if (!books.Any())
                return NotFound("No books found in this series.");

            return Ok(books);
        }
    }
}
