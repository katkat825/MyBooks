using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using MyBooks.Common.Helpers;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Text.Json;

namespace MyBooks.FileService.Controllers;

[ApiController]
[Route("api/bulk-import")]
[Authorize(Roles = AppRoles.OwnerPlus)]
public class BulkImportController : ControllerBase
{
    private readonly FileDbContext _context;
    private readonly SystemTokenHelper _systemTokenHelper;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public BulkImportController(
        FileDbContext context,
        SystemTokenHelper systemTokenHelper,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _context = context;
        _systemTokenHelper = systemTokenHelper;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    // Start a new bulk import job
    [HttpPost("start")]
    public async Task<IActionResult> StartImport([FromBody] List<BookImportRequestDto> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files provided for import.");

        var tenantId = _context.GetCurrentTenantId();
        var userId = _context.GetCurrentUserId();
        var ipAddress = _context.GetCurrentUserIpAddress();

        // Create a new job
        var job = new BulkImportJob
        {
            TenantId = tenantId,
            Status = "Running",
            TotalFiles = files.Count,
            ProcessedFiles = 0,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        // Add job + items
        foreach (var f in files)
        {
            job.Items.Add(new BulkImportItem
            {
                FileId = f.FilePath, // Google Drive fileId
                FileName = f.FileName,
                Status = "Pending",
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            });
        }

        _context.BulkImportJobs.Add(job);
        await _context.SaveChangesAsSystemAsync();

        // Process inline (later replace with Hangfire)
        await ProcessJobAsync(job.Id, files, userId, ipAddress, tenantId);

        return Ok(new { JobId = job.Id, job.Status });
    }

    // Check job status
    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetStatus(int jobId)
    {
        var job = await _context.BulkImportJobs
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null) return NotFound();

        return Ok(new
        {
            job.Id,
            job.Status,
            job.TotalFiles,
            job.ProcessedFiles,
            job.ErrorMessage,
            Items = job.Items.Select(i => new
            {
                i.Id,
                i.FileName,
                i.Status,
                i.CreatedBookId,
                i.CreatedFileId,
                i.ErrorMessage
            })
        });
    }

    // --- Private helpers ---

    private async Task ProcessJobAsync(int jobId, List<BookImportRequestDto> files, string userId, string ipAddress, int tenantId)
    {
        var job = await _context.BulkImportJobs
            .Include(j => j.Items)
            .FirstAsync(j => j.Id == jobId);

        try
        {
            // Get system token for CatalogService
            var token = await _systemTokenHelper.GetSystemTokenAsync(
                "FileService",
                _config["ServiceSecrets:FileService"]);

            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            foreach (var item in job.Items)
            {
                var dto = files.First(f => f.FileName == item.FileName);

                try
                {
                    // 1. Create Book in CatalogService
                    var catalogUrl = $"{_config["ServiceUrls:CatalogService"]}/api/book-import";
                    var bookResp = await http.PostAsync(catalogUrl,
                        new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));

                    if (!bookResp.IsSuccessStatusCode)
                        throw new Exception(await bookResp.Content.ReadAsStringAsync());

                    var bookDto = await bookResp.Content.ReadFromJsonAsync<BookImportResponseDto>();
                    if (bookDto == null)
                        throw new Exception("Book creation returned null response.");

                    // 2. Save FileMetadata in FileService
                    var integration = await _context.GoogleIntegrations
                        .FirstOrDefaultAsync(g => g.TenantId == tenantId && g.IsActive);

                    if (integration == null)
                        throw new Exception("No active Google Drive integration for tenant.");

                    var fileMetadata = new FileMetadata
                    {
                        TenantId = tenantId,
                        GoogleIntegrationId = integration.Id,
                        FolderId = null,
                        FileName = dto.FileName,
                        FilePath = dto.FilePath, // Google Drive fileId
                        ContentType = dto.FilePath.EndsWith(".epub")
                            ? "application/epub+zip"
                            : "application/pdf",
                        FileSize = 0, // could be retrieved from Google API if needed
                        BookId = bookDto.BookId,
                        IsActive = true,
                    };

                    _context.Files.Add(fileMetadata);
                    await _context.SaveChangesAsSystemAsync(userId, ipAddress);

                    // 3. Patch CatalogService to attach FileId
                    var attachDto = new BookFileLinkDto
                    {
                        BookId = bookDto.BookId,
                        FileId = fileMetadata.Id
                    };

                    var attachResp = await http.PatchAsync(
                        $"{_config["ServiceUrls:CatalogService"]}/api/book-import/file",
                        new StringContent(JsonSerializer.Serialize(attachDto), Encoding.UTF8, "application/json"));

                    if (!attachResp.IsSuccessStatusCode)
                        throw new Exception(await attachResp.Content.ReadAsStringAsync());

                    // 4. Update job item
                    item.Status = "Success";
                    item.CreatedBookId = bookDto.BookId;
                    item.CreatedFileId = fileMetadata.Id;
                }
                catch (Exception ex)
                {
                    item.Status = "Failed";
                    item.ErrorMessage = ex.Message;
                }

                job.ProcessedFiles++;
                await _context.SaveChangesAsSystemAsync();
            }

            job.Status = "Completed";
            await _context.SaveChangesAsSystemAsync();
        }
        catch (Exception ex)
        {
            job.Status = "Failed";
            job.ErrorMessage = ex.Message;
            await _context.SaveChangesAsSystemAsync();
        }
    }
}
