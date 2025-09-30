using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyBooks.Common.BaseClasses;
using MyBooks.SupportService.Data;
using MyBooks.SupportService.Models;
using System.Security.Claims;

namespace MyBooks.SupportService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.SuperAdmin)]
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
    public async Task<IActionResult> Start(int targetUserId)
    {
        var impersonatingUserId = int.Parse(
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Authenticated user id not found")
        );

        var log = new ImpersonationLog
        {
            TargetUserId = targetUserId,
            ImpersonatingUserId = impersonatingUserId,
            StartTime = DateTime.UtcNow
        };

        _context.ImpersonationLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(log.Id);
    }

    // stop impersonation
    [HttpPost("stop/{id}")]
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
