using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;
using MyBooks.Common.Services;

namespace MyBooks.CatalogService.Controllers
{
    [Route("api/books/genres")]
    [ApiController]
    //[Authorize(Roles = "Admin")] 
    public class GenreController : ControllerBase
    {
        private readonly CatalogDbContext _context;
        private readonly HtmlSanitizationService _htmlSanitizationService;

        public GenreController(CatalogDbContext context, HtmlSanitizationService htmlSanitizationService)
        {
            _context = context;
            _htmlSanitizationService = htmlSanitizationService;
        }

        //get all genres
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Genre>>> GetGenres()
        {
            return await _context.Genres.ToListAsync();
        }

        // add a new genre
        [HttpPost]
        public async Task<ActionResult<Genre>> AddGenre(Genre genre)
        {
            if (genre == null) return BadRequest("Invalid genre data.");

            genre.Name = _htmlSanitizationService.Sanitize(genre.Name).Trim();

            //disallow duplicates
            var existingGenre = await _context.Genres
                .Where(g => g.Name.ToLower() == genre.Name.ToLower())
                .FirstOrDefaultAsync();

            if (existingGenre != null) 
                return Conflict($"A genre with the name {genre.Name} already exists.");

            //add createdby info
            genre.CreatedBy = User.Identity.Name;
            genre.CreatedDate = DateTime.UtcNow;

            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGenres), new { id = genre.Id }, genre);
        }

        //update genre
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGenre(int id, Genre genre)
        {
            if (id != genre.Id)
                return BadRequest("Genre ID mismatch.");

            var existingGenre = await _context.Genres.FindAsync(id);

            if (existingGenre == null)
                return NotFound("Genre not found.");

            genre.Name = _htmlSanitizationService.Sanitize(genre.Name).Trim();

            //disallow duplicates
            var dupilcateGenre = await _context.Genres
                .Where(g => g.Name.ToLower() == genre.Name.ToLower())
                .FirstOrDefaultAsync();

            if (dupilcateGenre != null)
                return Conflict($"A genre with the name {genre.Name} already exists.");

            //update fields
            existingGenre.Name = genre.Name;
            existingGenre.LastModifiedBy = User.Identity.Name;
            existingGenre.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres
                .Include(g => g.Books) //check for genre in use
                .FirstOrDefaultAsync(g => g.Id == id);

            if (genre == null) return NotFound("Genre not found.");

            if (genre.Books != null && genre.Books.Any())
                return Conflict("Cannot delete this genre because it is in use.");

            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
