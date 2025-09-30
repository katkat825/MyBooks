using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using MyBooks.Common.Helpers;

namespace MyBooks.FileService.Controllers;

[ApiController]
[Route("api/support/[controller]")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class SupportFileController : ControllerBase
{
    private readonly FileDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly SystemTokenHelper _tokenHelper;

    public SupportFileController(
        FileDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        SystemTokenHelper tokenHelper)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
        _config = config;
        _tokenHelper = tokenHelper;
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