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
[Route("")]
[Authorize(Roles = AppRoles.OwnerPlus)]
public class TenantController : ControllerBase
{
    private readonly TenantDbContext _context;

    public TenantController(TenantDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TenantUpdateDto>> GetTenant(int id)
    {
        var tenant = await _context.Tenants
            .Include(t => t.BillingPlan)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
        {
            return NotFound();
        }

        var claims = User.ToJwtClaimsDto();
        if (tenant.Id != claims.TenantId)
        {
            return Forbid();
        }

        return new TenantUpdateDto
        {
            BillingPlanId = tenant.BillingPlanId
        };
    }

    [HttpGet("{id}/anon")]
    [AllowAnonymous] // useful for login flow
    public async Task<ActionResult<TenantReadDto>> GetTenantById(int id)
    {
        var tenant = await _context.Tenants
            .Include(t => t.BillingPlan)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
            return NotFound();

        return new TenantReadDto
        {
            Id = tenant.Id,
            IsActive = tenant.IsActive,
            MaxStorageMb = tenant.BillingPlan.MaxStorageMb
        };
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTenant(int id, TenantUpdateDto tenantDto)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
        {
            return NotFound();
        }

        var claims = User.ToJwtClaimsDto();
        if (tenant.Id != claims.TenantId)
        {
            return Forbid();
        }
        
        tenant.BillingPlanId = tenantDto.BillingPlanId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // deactivate
    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult<Tenant>> DeactivateTenant(int id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
        {
            return NotFound();
        }

        var claims = User.ToJwtClaimsDto();
        if (tenant.Id != claims.TenantId)
        {
            return Forbid();
        }

        tenant.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // reactivate
    [HttpPatch("{id}/activate")]
    public async Task<ActionResult<Tenant>> ActivateTenant(int id)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
        {
            return NotFound();
        }

        tenant.IsActive = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
