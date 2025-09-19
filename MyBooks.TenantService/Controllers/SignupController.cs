using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using MyBooks.Common.Services;
using MyBooks.TenantService.Data;
using MyBooks.TenantService.Models;
using MyBooks.TenantService.Services;

namespace MyBooks.TenantService.Controllers;

[ApiController]
[Route("api/signup")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class SignupController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly AuthClient _auth;
    private readonly HtmlSanitizationService _sanitizationService;
    private readonly CatalogClient _catalog;

    public SignupController(TenantDbContext context, AuthClient auth, HtmlSanitizationService sanitizationService, CatalogClient catalog)
    {
        _context = context;
        _auth = auth;
        _sanitizationService = sanitizationService;
        _catalog = catalog;
    }

    // user request to create portal
    [HttpPost]
    public async Task<ActionResult<SignupResponseDto>> Signup(SignupRequestDto request)
    {
        Console.WriteLine("signup controller reached");
        // sanitize all open text fields
        request.FirstName = _sanitizationService.Sanitize(request.FirstName);
        request.LastName = _sanitizationService.Sanitize(request.LastName);
        request.Email = _sanitizationService.Sanitize(request.Email, true);

        // create user with no tenantid
        var user = new UserDto
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = request.Password,
            Role = AppRoles.Owner,
            AgeCategoryId = 3,
            IsActive = true,            
        };

        Console.WriteLine("attempting to create user");
        var userId = await _auth.CreateUserAsync(user);
        Console.WriteLine("user created");

        // create tenant
        var tenant = new Tenant
        {
            BillingPlanId = request.BillingPlanId ?? 1,
            OwnerUserId = userId,
            IsActive = true,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };

        Console.WriteLine("attempting to add tenant");
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsSystemAsync();
        Console.WriteLine("tenant added successfully");

        // try seeding default genres
        try
        {
            await _catalog.SeedDefaultGenresAsync(tenant.Id);
            Console.WriteLine("genres seeding successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Failed to seed genres for tenant {tenant.Id}: {ex.Message}");
        }

        Console.WriteLine("attempting to assign tenant to user");

        // add tenantid to user record
        await _auth.AssignTenantAsync(new AssignTenantDto
        {
            UserId = userId,
            TenantId = tenant.Id
        });

        return Ok(new SignupResponseDto
        {
            TenantId = tenant.Id,
            OwnerUserId = userId
        });
    }
}