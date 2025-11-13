
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.BaseClasses;
using MyBooks.TenantService.Data;

namespace MyBooks.TenantService.Controllers;

[ApiController]
[Route("internaltenant")]
public class InternalTenantController : ControllerBase
{
    public readonly TenantDbContext _context;

    public InternalTenantController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet("{tenantId}/max-user-count")]
    [Authorize(Roles = AppRoles.AuthService)]
    public async Task<IActionResult> GetMaxUserCount (int tenantId)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == tenantId)
            .FirstOrDefaultAsync();

        if (tenant == null)
            return NotFound();
        
        return Ok(tenant.MaxUserCount);
    }
}