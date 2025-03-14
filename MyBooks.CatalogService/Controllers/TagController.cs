using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Data;
using MyBooks.CatalogService.Models;
using MyBooks.Common.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyBooks.CatalogService.Controllers
{
    [Route("api/books/tag")]
    [ApiController]
    [Authorize]
    public class TagController : ControllerBase
    {
        private readonly CatalogDbContext _context;
        private readonly HtmlSanitizationService _htmlSanitizationService;

        public TagController(CatalogDbContext context, HtmlSanitizationService htmlSanitizationService)
        {
            _context = context;
            _htmlSanitizationService = htmlSanitizationService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tag>>> GetTags()
        {
            return await _context.Tags.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Tag>> AddTag(Tag tag)
        {
            if (string.IsNullOrWhiteSpace(tag.Name))
                return BadRequest("Tag name is required.");

            tag.Name = _htmlSanitizationService.Sanitize(tag.Name).Trim().ToLower();

            // Disallow duplicates
            var existingTag = await _context.Tags
                .Where(t => t.Name == tag.Name)
                .FirstOrDefaultAsync();

            if (existingTag != null)
                return Conflict($"A tag with the name '{tag.Name}' already exists.");

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTags), new { id = tag.Id }, tag);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, Tag tag)
        {
            if (id != tag.Id)
                return BadRequest("Tag ID mismatch.");

            var existingTag = await _context.Tags.FindAsync(id);
            if (existingTag == null)
                return NotFound("Tag not found.");

            tag.Name = _htmlSanitizationService.Sanitize(tag.Name).Trim().ToLower();

            // Check for duplicate name
            var duplicateTag = await _context.Tags
                .Where(t => t.Name == tag.Name && t.Id != id)
                .FirstOrDefaultAsync();

            if (duplicateTag != null)
                return Conflict($"A tag with the name '{tag.Name}' already exists.");

            existingTag.Name = tag.Name;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            var tag = await _context.Tags
                .Include(t => t.Books) // Check if tag is used
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag == null)
                return NotFound("Tag not found.");

            if (tag.Books != null && tag.Books.Any())
                return Conflict("Cannot delete this tag because it is in use.");

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
