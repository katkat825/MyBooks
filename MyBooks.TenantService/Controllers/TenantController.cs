using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.BaseClasses;
using MyBooks.TenantService.Data;
using MyBooks.TenantService.Dtos;
using MyBooks.TenantService.Models;

namespace MyBooks.TenantService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TenantController : ControllerBase
    {
        private readonly TenantDbContext _context;

        public TenantController(TenantDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetAllTenants()
        {
            return await _context.Tenants
                .Include(t => t.BillingPlan)
                .AsNoTracking()
                .ToListAsync();
        }

        [HttpGet("{id}")]
        [Authorize(Roles = AppRoles.OwnerPlus)]
        public async Task<ActionResult<TenantUpdateDto>> GetTenant(int id)
        {
            var tenant = await _context.Tenants
                .Include(t => t.BillingPlan)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null)
            {
                return NotFound();
            }

            var userTenantId = User.FindFirst("TenantId")?.Value;
            if (userTenantId == null || tenant.Id.ToString() != userTenantId)
            {
                return Forbid();
            }

            return new TenantUpdateDto
            {
                Name = tenant.Name,
                BillingPlanId = tenant.BillingPlanId
            };
        }

        [HttpGet("by-subdomain/{subdomain}")]
        [AllowAnonymous] // useful for login flow
        public async Task<ActionResult<TenantReadDto>> GetTenantBySubdomain(string subdomain)
        {
            var tenant = await _context.Tenants
                .Include(t => t.BillingPlan)
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain);

            if (tenant == null)
                return NotFound();

            return new TenantReadDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Subdomain = tenant.Subdomain,
                IsActive = tenant.IsActive,
                MaxStorageMb = tenant.BillingPlan.MaxStorageMb
            };
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<ActionResult<Tenant>> CreateTenant(TenantCreateDto tenantDto)
        {
            var tenant = new Tenant
            {
                Name = tenantDto.Name,
                Subdomain = tenantDto.Subdomain,
                BillingPlanId = tenantDto.BillingPlanId,
                OwnerUserId = tenantDto.OwnerUserId,
                IsActive = true
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, tenant);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.OwnerPlus)]
        public async Task<IActionResult> UpdateTenant(int id, TenantUpdateDto tenantDto)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            var userTenantId = User.FindFirst("TenantId")?.Value;
            if (userTenantId == null || tenant.Id.ToString() != userTenantId)
            {
                return Forbid();
            }

            tenant.Name = tenantDto.Name;
            tenant.BillingPlanId = tenantDto.BillingPlanId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // deactivate
        [HttpPatch("{id}/deactivate")]
        [Authorize(Roles = AppRoles.OwnerPlus)]
        public async Task<ActionResult<Tenant>> DeactivateTenant(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }

            var userTenantId = User.FindFirst("TenantId")?.Value;
            if (userTenantId == null || tenant.Id.ToString() != userTenantId)
            {
                return Forbid();
            }

            tenant.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // reactivate
        [HttpPatch("{id}/activate")]
        [Authorize(Roles = AppRoles.SuperAdmin)]
        public async Task<ActionResult<Tenant>> ActivateTenant(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }
            tenant.IsActive = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}