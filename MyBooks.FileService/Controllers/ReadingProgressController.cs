using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.Services;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Validators;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Microsoft.Extensions.Configuration.UserSecrets;
using MyBooks.Common.Dtos;

[Route("progress")]
[ApiController]
[Authorize]
public class ReadingProgressController : ControllerBase
{
    private readonly FileDbContext _context;

    public ReadingProgressController(FileDbContext context)
    {
        _context = context;
    }

    // get all recently read
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentProgress([FromQuery] int userId, [FromQuery] int count = 10)
    {
        if (userId <= 0)
            return BadRequest("A valid user ID is required");
        try
        {
            var recentProgress = await _context.ReadingProgresses
                    .IgnoreQueryFilters()
                    .Where(r => r.UserId == userId && r.ProgressPercent < 99)
                    .OrderByDescending(r => r.LastUpdated)
                    .Take(count)
                    .Select(r => new ReadingProgressDto
                    {
                        FileId = r.FileId,
                        ProgressPercent = r.ProgressPercent,
                        LastUpdated = r.LastUpdated
                    })
                    .ToListAsync();

            return Ok(recentProgress);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching recent progress for user {userId}: {ex.Message}");
            return StatusCode(500, "An error occurred while retrieving recent reading progress.");
        }
    }

    [HttpGet("{fileId}")]
    public async Task<IActionResult> GetReadingProgress(int fileId)
    {
        var userClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userClaim) || !int.TryParse(userClaim, out int userId))
            return Unauthorized("User not identified");

        var progress = await _context.ReadingProgresses
            .FirstOrDefaultAsync(r => r.FileId == fileId && r.UserId == userId);

        if (progress == null)
            return Ok(new { ProgressPercent = 0 });

        return Ok(progress);
    }

    [HttpPost("{fileId}")]
    public async Task<IActionResult> UpdateReadingProgress(int fileId, [FromBody] ReadingProgressUpdateDto dto)
    {
        var userClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userClaim) || !int.TryParse(userClaim, out int userId))
            return Unauthorized("User not identified.");

        if (dto.ProgressPercent < 0 || dto.ProgressPercent > 100)
            return BadRequest("ProgressPercent must be between 0 and 100.");

        var progress = await _context.ReadingProgresses
            .FirstOrDefaultAsync(r => r.FileId == fileId && r.UserId == userId);

        if (progress == null)
        {
            progress = new ReadingProgress
            {
                FileId = fileId,
                UserId = userId,
                ProgressPercent = dto.ProgressPercent,
                LastUpdated = DateTime.UtcNow
            };
            _context.ReadingProgresses.Add(progress);
        }
        else
        {
            progress.ProgressPercent = dto.ProgressPercent;
            progress.LastUpdated = DateTime.UtcNow;
            _context.ReadingProgresses.Update(progress);
        }

        await _context.SaveChangesAsync();
        return Ok(progress);
    }
}
