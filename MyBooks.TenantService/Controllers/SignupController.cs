using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
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

        var userId = await _auth.CreateUserAsync(user);

        // create tenant
        var tenant = new Tenant
        {
            BillingPlanId = request.BillingPlanId,
            OwnerUserId = userId,
            IsActive = true,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsSystemAsync();

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