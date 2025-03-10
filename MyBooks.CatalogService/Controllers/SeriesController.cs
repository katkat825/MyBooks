using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;
using MyBooks.Common.Services;

namespace MyBooks.CatalogService.Controllers
{
    [Route("api/books/series")]
    [ApiController]
    //[Authorize]
    public class SeriesController : ControllerBase
    {
        private readonly CatalogDbContext _context;
        private readonly HtmlSanitizationService _sanitizationService;

        public SeriesController(CatalogDbContext context, HtmlSanitizationService htmlSanitizationService)
        {
            _context = context;
            _sanitizationService = htmlSanitizationService;
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

            series.Name = _sanitizationService.Sanitize(series.Name.Trim());

            //disallow duplicates
            var existingSeries = await _context.Series
                .Where(s => s.Name.ToLower() == series.Name.ToLower())
                .FirstOrDefaultAsync();

            if (existingSeries != null) return Conflict($"A series with the name '{series.Name}' already exists.");

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

            updatedSeries.Name = _sanitizationService.Sanitize(updatedSeries.Name.Trim());

            //disallow duplicates
            var duplicateSeries = await _context.Series
                .Where(s => s.Id != id && s.Name.ToLower() == updatedSeries.Name.ToLower())
                .FirstOrDefaultAsync();

            if (duplicateSeries != null)
                return Conflict($"A series with the name '{updatedSeries.Name}' already exists.");

            series.Name = updatedSeries.Name;

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

        //delete series
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSeries(int id)
        {
            var series = await _context.Series
                .Include(s => s.Books) // Include books related to the series
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null)
                return NotFound("Series not found.");

            if (series.Books != null && series.Books.Any())
                return Conflict("Cannot delete this series because it contains books.");

            _context.Series.Remove(series);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
