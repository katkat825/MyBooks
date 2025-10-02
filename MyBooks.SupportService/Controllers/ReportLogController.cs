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
    public async Task<IActionResult> Update(int id, [FromBody] ReportLog updatedReport)
    {
        if (id != updatedReport.Id)
        {
            return BadRequest();
        }

        var existing = await _context.ReportLogs.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        // update allowed fields
        existing.Status = updatedReport.Status;
        existing.Resolution = updatedReport.Resolution;
        existing.ResolutionNotes = updatedReport.ResolutionNotes;
        existing.DateClosed = updatedReport.DateClosed;
        existing.TargetType = updatedReport.TargetType;
        existing.TargetId = updatedReport.TargetId;
        existing.TargetCreatedBy = updatedReport.TargetCreatedBy;
        existing.DateClosed = updatedReport.DateClosed;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
