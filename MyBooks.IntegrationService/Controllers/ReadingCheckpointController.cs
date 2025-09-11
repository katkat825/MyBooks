using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.IntegrationService.Data;
using MyBooks.IntegrationService.Models;

namespace MyBooks.IntegrationService.Controllers;

[ApiController]
[Route("api/reading-checkpoints")]
[Authorize]
public class ReadingCheckpointController : ControllerBase
{
    private readonly IntegrationDbContext _context;

    public ReadingCheckpointController(IntegrationDbContext context)
    {
        _context = context;
    }

    // GET api/reading-checkpoints/{bookId}
    [HttpGet("{bookId}")]
    public async Task<IActionResult> GetCheckpoint(int bookId)
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);

        var checkpoint = await _context.ReadingCheckpoints
            .FirstOrDefaultAsync(rc => rc.BookId == bookId && rc.UserId == userId);

        if (checkpoint == null)
            return NotFound();

        return Ok(checkpoint);
    }

    // POST api/reading-checkpoints
    [HttpPost]
    public async Task<IActionResult> SaveCheckpoint([FromBody] ReadingCheckpoint checkpoint)
    {
        var userId = int.Parse(User.FindFirst("sub")!.Value);
        checkpoint.UserId = userId;
        checkpoint.LastReadAt = DateTime.UtcNow;
        checkpoint.TenantId = int.Parse(User.FindFirst("TenantId")!.Value);

        var existing = await _context.ReadingCheckpoints
            .FirstOrDefaultAsync(rc => rc.BookId == checkpoint.BookId && rc.UserId == userId);

        if (existing == null)
        {
            _context.ReadingCheckpoints.Add(checkpoint);
        }
        else
        {
            existing.LastPage = checkpoint.LastPage;
            existing.LastReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(checkpoint);
    }
}
