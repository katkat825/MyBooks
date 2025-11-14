
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.BaseClasses;
using MyBooks.TenantService.Data;

namespace MyBooks.TenantService.Controllers;

[ApiController]
[Route("system")]
[Authorize(Roles = AppRoles.AuthService)]
public class SystemTenantController : ControllerBase
{
    public readonly TenantDbContext _context;

    public SystemTenantController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}/max-users")]
    public async Task<IActionResult> GetMaxUserCount (int id)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == id)
            .FirstOrDefaultAsync();

        if (tenant == null)
            return NotFound();
        
        return Ok(tenant.MaxUserCount);
    }
}