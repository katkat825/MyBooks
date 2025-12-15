using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Dtos;
using MyBooks.AuthService.Models;
using MyBooks.AuthService.Data;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Services;
using MyBooks.Common.Dtos;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using MyBooks.AuthService.Services;

namespace MyBooks.AuthService.Controllers;

[ApiController]
[Route("system/users")]
[Authorize(Roles = AppRoles.TenantService)]
public class SystemUsersController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly HtmlSanitizationService _sanitizationService;
    private readonly InvitationService _invitationService;

    public SystemUsersController(AuthDbContext context, HtmlSanitizationService sanitizationService, InvitationService invitationService)
    {
        _context = context;
        _sanitizationService = sanitizationService;
        _invitationService = invitationService;
    }

    // new user, no tenant yet
    [HttpPost("create")]
    public async Task<IActionResult> CreateUser(OwnerUserDto request)
    {
        Console.WriteLine("[SystemUsers] CreateUser called");
        
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { message = "Email already in use." });

        var userRole = string.IsNullOrEmpty(request.Role) ? AppRoles.Owner : request.Role;

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Role = userRole,
            AgeCategoryId = 3,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow,
            TenantId = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsSystemAsync();

        await _invitationService.CreateAndSendNewAccountEmailAsync(user.Id);

        return Ok(new CreatedUserResponseDto
        {
            UserId = user.Id,
            Created = true
        });
    }

    [HttpPost("tenant")]
    public async Task<IActionResult> AssignTenant(AssignTenantDto request)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (user == null)
        {
            return NotFound(new { message = $"User {request.UserId} not found" });
        }

        if (user.TenantId != null)
        {
            return BadRequest(new { message = $"User already assigned to tenant: {user.TenantId}" });
        }

        user.TenantId = request.TenantId;
         _context.Entry(user).Property(u => u.TenantId).IsModified = true;

        try
        {
            await _context.SaveChangesAsSystemAsync();
        }
        catch (Exception ex)
        {
            throw; 
        }

        return NoContent();
    }
}