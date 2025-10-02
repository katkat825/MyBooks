using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.SupportService.Data;
using MyBooks.SupportService.Models;
using MyBooks.Common.BaseClasses;
using Microsoft.Identity.Client;

namespace MyBooks.SupportService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.SuperAdmin)]
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

        // only update the fields that are allowed in the DTO
        if (dto.Status != null) existing.Status = dto.Status;
        if (dto.Resolution != null) existing.Resolution = dto.Resolution;
        if (dto.ResolutionNotes != null) existing.ResolutionNotes = dto.ResolutionNotes;
        if (dto.DateClosed != null) existing.DateClosed = DateTime.Parse(dto.DateClosed);
        if (dto.TargetType != null) existing.TargetType = dto.TargetType;
        if (dto.TargetId.HasValue) existing.TargetId = dto.TargetId;
        if (dto.TargetCreatedBy != null) existing.TargetCreatedBy = dto.TargetCreatedBy;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
