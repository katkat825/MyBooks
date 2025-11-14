using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Helpers;
using MyBooks.TenantService.Data;
using MyBooks.TenantService.Dtos;
using MyBooks.TenantService.Models;

namespace MyBooks.TenantService.Controllers;

[ApiController]
[Route("support")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class SupportTenantController : ControllerBase
{
    private readonly TenantDbContext _context;

    public SupportTenantController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<ActionResult<IEnumerable<Tenant>>> GetAllTenants()
    {
        return await _context.Tenants
            .IgnoreQueryFilters()
            .Include(t => t.BillingPlan)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TenantUpdateDto>> GetTenant(int id)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .Include(t => t.BillingPlan)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
        {
            return NotFound();
        }

        return new TenantUpdateDto
        {
            BillingPlanId = tenant.BillingPlanId
        };
    }
    
    [HttpPost]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<ActionResult<Tenant>> CreateTenant(TenantCreateDto tenantDto)
    {
        var tenant = new Tenant
        {
            BillingPlanId = tenantDto.BillingPlanId,
            OwnerUserId = tenantDto.OwnerUserId,
            IsActive = true
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> UpdateTenantActiveStatus(int id, [FromBody] bool isActive)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
            return NotFound();

        tenant.IsActive = isActive;
        await _context.SaveChangesAsync();

        return NoContent();
    }

}