using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.SupportService.Data;
using MyBooks.SupportService.Models;
using MyBooks.Common.BaseClasses;
using Microsoft.Identity.Client;

namespace MyBooks.SupportService.Controllers;

[ApiController]
[Route("logs/violations")]
[Authorize(Roles = AppRoles.SuperAdmin + "," + AppRoles.GlobalReviewer)]
public class ReportLogController : ControllerBase
{
    private readonly SupportDbContext _context;

    public ReportLogController(SupportDbContext context)
    {
        _context = context;
    }

    // get all reports
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReportLog>>> GetAll()
    {
        return await _context.ReportLogs.AsNoTracking().ToListAsync();
    }

    // get by id
    [HttpGet("{id}")]
    public async Task<ActionResult<ReportLog>> GetById(int id)
    {
        var report = await _context.ReportLogs.FindAsync(id);
        if (report == null)
        {
            return NotFound();
        }
        return report;
    }

    // create
    [HttpPost]
    public async Task<ActionResult<ReportLog>> Create([FromBody] ReportLog report)
    {
        _context.ReportLogs.Add(report);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = report.Id }, report);
    }

    // update (status, resolution, notes, etc.)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateReportLogDto dto)
    {
        var existing = await _context.ReportLogs.FindAsync(id);
        if (existing == null) return NotFound();

        // status - keep old if not provided
        existing.Status = dto.Status ?? existing.Status;

        // these can be updated or cleared (set to null)
        existing.Resolution = dto.Resolution;
        existing.ResolutionNotes = dto.ResolutionNotes;
        existing.TargetType = dto.TargetType;
        existing.TargetId = dto.TargetId;
        existing.TargetCreatedBy = dto.TargetCreatedBy;
        existing.ReviewNotes = dto.ReviewNotes;

        // dateClosed: parse if provided, clear if null/empty
        existing.DateClosed = string.IsNullOrEmpty(dto.DateClosed)
            ? null
            : DateTime.Parse(dto.DateClosed);

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
