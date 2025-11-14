using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using MyBooks.SupportService.Data;
using MyBooks.SupportService.Models;
using System.Security.Claims;

namespace MyBooks.SupportService.Controllers;

[ApiController]
[Route("logs/impersonations")]
[Authorize(Roles = AppRoles.AuthService)]
public class ImpersonationLogController : ControllerBase
{
    private readonly SupportDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ImpersonationLogController(SupportDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    // start impersonation
    [HttpPost("start")]
    public async Task<IActionResult> Start(ImpersonationDto dto)
    {
        var log = new ImpersonationLog
        {
            TargetUserId = dto.TargetUserId,
            ImpersonatingUserId = dto.ImpersonatingUserId,
            StartTime = DateTime.UtcNow
        };

        _context.ImpersonationLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(log.Id);
    }

    // stop impersonation
    [HttpPost("{id}/stop")]
    public async Task<IActionResult> Stop(int id)
    {
        var log = await _context.ImpersonationLogs.FindAsync(id);
        if (log == null)
        {
            return NotFound();
        }

        log.EndTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
