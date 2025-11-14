using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Services;

namespace MyBooks.FileService.Controllers;

[ApiController]
[Route("import")]
[Authorize(Roles = AppRoles.OwnerPlus)]
public class BulkImportController : ControllerBase
{
    private readonly FileDbContext _context;
    private readonly BulkImportProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;

    public BulkImportController(FileDbContext context, BulkImportProcessor processor, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _processor = processor;
        _scopeFactory = scopeFactory;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartBulkImport([FromBody] BulkImportStartDto dto)
    {
        if (dto == null || dto.FileIds.Count == 0)
            return BadRequest("No files provided for bulk import");

        var tenantId = _context.GetCurrentTenantId();
        var userId = _context.GetCurrentUserId();
        var ip = _context.GetCurrentUserIpAddress();

        // create bulk import job
        var job = new BulkImportJob
        {
            TenantId = tenantId,
            Status = "Pending",
            TotalFiles = dto.FileIds.Count,
            GoogleIntegrationId = dto.IntegrationId,
            ProcessedFiles = 0,
            CreatedBy = userId,
            CreatedDate = DateTime.UtcNow
        };

        _context.BulkImportJobs.Add(job);
        await _context.SaveChangesAsSystemAsync();

        // create bulk import items
        foreach (var fileId in dto.FileIds)
        {
            var overrideEntry = dto.Overrides?.FirstOrDefault(o => o.FileId == fileId);

            var item = new BulkImportItem
            {
                BulkImportJobId = job.Id,
                TenantId = tenantId,
                FileId = fileId,
                FileName = null, // filled in later during filescan
                GenreId = overrideEntry?.GenreId ?? dto.GenreId,
                AgeCategoryId = overrideEntry?.AgeCategoryId ?? dto.AgeCategoryId,
                Status = "Pending",
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };
            _context.BulkImportItems.Add(item);
        }

        await _context.SaveChangesAsSystemAsync();

        // prepare FileScanDtos 
        var scanDto = new FileScanDto
        {
            UserId = userId,
            TenantId = tenantId,
            IpAddress = ip,
            BulkImportStart = dto,
            IntegrationId = dto.IntegrationId
        };

        // call filescan process
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<BulkImportProcessor>();
            await processor.ProcessJobAsync(job.Id, scanDto);
        });

        return Ok();
    }

    [HttpGet("{id}/status}")]
    public async Task<IActionResult> GetJobStatus(int id)
    {
        var job = await _context.BulkImportJobs
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound();

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
                i.FileId,
                i.FileName,
                i.GenreId,
                i.AgeCategoryId,
                i.Status,
                i.ErrorMessage,
                i.CreatedBookId,
                i.CreatedFileId
            })
        });
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetAllJobs()
    {
        var tenantId = _context.GetCurrentTenantId();

        var jobs = await _context.BulkImportJobs
            .Include(j => j.Items)
            .Where(j => j.TenantId == tenantId)
            .OrderByDescending(j => j.CreatedDate)
            .Take(20)
            .ToListAsync();

        var shaped = jobs.Select(j => new {
            j.Id,
            j.Status,
            j.TotalFiles,
            j.ProcessedFiles,
            j.CreatedDate,
            CompletedDate = j.LastModifiedDate,
            j.ErrorMessage,
            Items = j.Items
                .Where(i => i.Status != "Success")
                .Select(i => new {
                    i.Id,
                    i.FileName,
                    i.Status,
                    i.ErrorMessage
                })
        });

        return Ok(shaped);
    }
}