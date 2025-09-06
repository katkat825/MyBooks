using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using MyBooks.Common.Dtos;
using MyBooks.Common.Services;
using MyBooks.TenantService.Data;
using MyBooks.TenantService.Models;
using MyBooks.TenantService.Services;

namespace MyBooks.TenantService.Controllers;

[ApiController]
[Route("api/signup")]
public class SignupController : ControllerBase
{
    private readonly TenantDbContext _context;
    private readonly AuthClient _auth;
    private readonly HtmlSanitizationService _sanitizationService;

    public SignupController(TenantDbContext context, AuthClient auth, HtmlSanitizationService sanitizationService)
    {
        _context = context;
        _auth = auth;
        _sanitizationService = sanitizationService;
    }

    // user request to create portal
    [HttpPost]
    public async Task<ActionResult<SignupResponseDto>> Signup(SignupRequestDto request)
    {
        // sanitize all open text fields
        request.FirstName = _sanitizationService.Sanitize(request.FirstName);
        request.LastName = _sanitizationService.Sanitize(request.LastName);
        request.Email = _sanitizationService.Sanitize(request.Email);
        request.Subdomain = _sanitizationService.Sanitize(request.Subdomain.ToLowerInvariant());
        request.TenantName = _sanitizationService.Sanitize(request.TenantName);

        // verify subdomain available
        var exists = await _context.Tenants.AnyAsync(t => t.Subdomain.ToLowerInvariant() == request.Subdomain);
        if (exists)
            return Conflict(new { message = $"Subdomain '{request.Subdomain}' is already in use." });
        var reserved = new[] { "www", "api", "admin" };
        if (reserved.Contains(request.Subdomain.ToLowerInvariant()))
            return BadRequest(new { message = $"Subdomain '{request.Subdomain}' is not allowed." });

        // create user with no tenantid
        var user = new UserDto
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Password = request.Password
        };

        var ownerUserId = await _auth.CreateUserAsync(user);

        // create tenant
        var tenant = new Tenant
        {
            Name = request.TenantName,
            Subdomain = request.Subdomain,
            BillingPlanId = request.BillingPlanId,
            OwnerUserId = ownerUserId,
            IsActive = true,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsSystemAsync();

        // add tenantid to user record
        await _auth.AssignTenantAsync(new AssignTenantDto
        {
            UserId = ownerUserId,
            TenantId = tenant.Id
        });

        var devUrl = $"http://{tenant.Subdomain}.localhost:62194";
        var portalUrl = $"https://{tenant.Subdomain}.mybookcatalog.com";

        return Ok(new SignupResponseDto
        {
            TenantId = tenant.Id,
            OwnerUserId = ownerUserId,
            PortalUrl = devUrl
        });
    }
}