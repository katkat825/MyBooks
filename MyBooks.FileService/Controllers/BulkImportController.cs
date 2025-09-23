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
[Route("api/bulk-import")]
[Authorize(Roles = AppRoles.OwnerPlus)]
public class BulkImportController : ControllerBase
{
    private readonly FileDbContext _context;
    private readonly BulkImportProcessor _processor;

    public BulkImportController(FileDbContext context, BulkImportProcessor processor)
    {
        _context = context;
        _processor = processor;
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
            BulkImportStart = dto
        };

        // call filescan process
        _ = Task.Run(() => _processor.ProcessJobAsync(job.Id, scanDto));

        return Ok(new
        {
            JobId = job.Id,
            Status = job.Status,
            TotalFiles = job.TotalFiles
        });
    }

    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetJobStatus(int jobId)
    {
        var job = await _context.BulkImportJobs
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == jobId);

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
}