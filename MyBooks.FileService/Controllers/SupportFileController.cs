using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Services;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using MyBooks.Common.Helpers;
using System.Security.Claims;

namespace MyBooks.FileService.Controllers;

[ApiController]
[Route("api/support/files")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class SupportFileController : ControllerBase
{
    private readonly FileDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly SystemTokenHelper _tokenHelper;
    private readonly GoogleDriveClient _googleDriveClient;

    public SupportFileController(
        FileDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        SystemTokenHelper tokenHelper,
        GoogleDriveClient googleDriveClient)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
        _tokenHelper = tokenHelper;
        _googleDriveClient = googleDriveClient;
    }

    // get all file metadata for a book (active + inactive)
    [HttpGet("book/{bookId}")]
    public async Task<ActionResult<IEnumerable<FileMetadata>>> GetAllFilesForBook(int bookId)
    {
        var files = await _context.Files
            .IgnoreQueryFilters()
            .Where(f => f.BookId == bookId)
            .AsNoTracking()
            .ToListAsync();

        return Ok(files);
    }

    // file download or read inline
    [HttpGet("{id}")]
    public async Task<IActionResult> DownloadFile(int id, [FromQuery] bool inline = false)
    {
        var file = await _context.Files
            .IgnoreQueryFilters()
            .Include(f => f.GoogleIntegration)
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

        if (file == null)
            return NotFound("File not found.");

        var stream = await _googleDriveClient.GetFileStreamAsync(
            file.FilePath, file.GoogleIntegration.RefreshToken);

        if (inline)
            return File(stream, file.ContentType);
        return File(stream, file.ContentType, file.FileName);
    }

    // get file metadata for a given file id
    [HttpGet("metadata/{id}")]
    public async Task<IActionResult> GetFileMetadata(int id)
    {
        var file = await _context.Files
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == id);
            
        if (file == null)
            return NotFound("File not found.");
        return Ok(file);
    }

    // GET api/files/progress/{fileId}
    [HttpGet("progress/{fileId}")]
    public async Task<IActionResult> GetReadingProgress(int fileId)
    {
        var userClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userClaim) || !int.TryParse(userClaim, out int userId))
            return Unauthorized("User not identified");

        var progress = await _context.ReadingProgresses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.FileId == fileId && r.UserId == userId);

        if (progress == null)
            return Ok(new { ProgressPercent = 0 });

        return Ok(progress);
    }

    // POST api/files/progress/{fileId}
    [HttpPost("progress/{fileId}")]
    public async Task<IActionResult> UpdateReadingProgress(int fileId, [FromBody] ReadingProgressUpdateDto dto)
    {
        var userClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userClaim) || !int.TryParse(userClaim, out int userId))
            return Unauthorized("User not identified.");

        if (dto.ProgressPercent < 0 || dto.ProgressPercent > 100)
            return BadRequest("ProgressPercent must be between 0 and 100.");

        var progress = await _context.ReadingProgresses
            .IgnoreQueryFilters()
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

    // flip active flag by duplicating the file row, and sync Book.FileId in CatalogService
    [HttpPatch("{fileId}/activate")]
    public async Task<IActionResult> ActivateFile(int fileId)
    {
        var file = await _context.Files
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == fileId);

        if (file == null)
            return NotFound("File not found.");

        // deactivate existing active file for this book
        var activeFile = await _context.Files
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.BookId == file.BookId && f.IsActive);

        if (activeFile != null && activeFile.Id != file.Id)
        {
            activeFile.IsActive = false;
            _context.Files.Update(activeFile);
        }

        // mark the original file inactive
        file.IsActive = false;
        _context.Files.Update(file);

        // duplicate the row with same tenant and mark as active
        var newFile = new FileMetadata
        {
            BookId = file.BookId,
            TenantId = file.TenantId,   // preserve original tenant
            FileName = file.FileName,
            FilePath = file.FilePath,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
            GoogleIntegrationId = file.GoogleIntegrationId,
            FolderId = file.FolderId,
            IsActive = true
        };

        _context.Files.Add(newFile);

        await _context.SaveChangesAsSupportAsync();

        // --- notify CatalogService ---
        var token = await _tokenHelper.GetSystemTokenAsync(
            "FileService",
            _config["ServiceSecrets:FileService"]);

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var catalogUrl = _config["ServiceUrls:CatalogService"];
        var dto = new BookFileLinkDto { BookId = newFile.BookId, FileId = newFile.Id };

        var response = await _httpClient.PatchAsJsonAsync(
            $"{catalogUrl}/api/support/book/{newFile.BookId}/file", dto);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode,
                $"Failed to update Book.FileId in CatalogService. {response.StatusCode}");
        }

        return NoContent();
    }
}