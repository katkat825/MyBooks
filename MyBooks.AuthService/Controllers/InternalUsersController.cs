using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Dtos;
using MyBooks.AuthService.Models;
using MyBooks.AuthService.Data;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Services;
using MyBooks.Common.Dtos;
using System.Security.Cryptography;

namespace MyBooks.AuthService.Controllers;

[ApiController]
[Route("api/internal/users")]
public class InternalUsersController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly HtmlSanitizationService _sanitizationService;

    public InternalUsersController(AuthDbContext context, HtmlSanitizationService sanitizationService)
    {
        _context = context;
        _sanitizationService = sanitizationService;
    }

    // new user, no tenant yet
    [HttpPost("create")]
    public async Task<IActionResult> Create(OwnerUserDto request)
    {
        Console.WriteLine("create user reached from tenantservice");

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { message = "Email already in use." });

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Role = AppRoles.Owner,
            AgeCategoryId = 3,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedBy = "System",
            CreatedDate = DateTime.UtcNow,
            TenantId = null
        };

        Console.WriteLine("attempting to add user");

        _context.Users.Add(user);
        await _context.SaveChangesAsSystemAsync();

        return Ok(new CreatedUserResponseDto
        {
            UserId = user.Id,
            Created = true
        });
    }

    [HttpPost("assign-tenant")]
    public async Task<IActionResult> AssignTenant(AssignTenantDto request)
    {
        Console.WriteLine("========== ASSIGN TENANT START ==========");
        Console.WriteLine($"Request received: UserId={request.UserId}, TenantId={request.TenantId}");

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (user == null)
        {
            Console.WriteLine($"DEBUG: No user found for UserId={request.UserId}");
            Console.WriteLine("========== ASSIGN TENANT END ==========");
            return NotFound(new { message = $"User {request.UserId} not found" });
        }

        Console.WriteLine($"DEBUG: Found user {user.Id}, current TenantId={user.TenantId}");

        if (user.TenantId != null)
        {
            Console.WriteLine($"DEBUG: User {user.Id} already has TenantId={user.TenantId}");
            Console.WriteLine("========== ASSIGN TENANT END ==========");
            return BadRequest(new { message = $"User already assigned to tenant: {user.TenantId}" });
        }

        user.TenantId = request.TenantId;
         _context.Entry(user).Property(u => u.TenantId).IsModified = true;

        Console.WriteLine($"DEBUG: Attempting to assign TenantId={request.TenantId} to UserId={user.Id}");

        try
        {
            await _context.SaveChangesAsSystemAsync();
            Console.WriteLine($"DEBUG: SUCCESS — User {user.Id} now assigned TenantId {user.TenantId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed saving TenantId assignment. Exception: {ex}");
            throw; // keep the stack trace for diagnostics
        }

        Console.WriteLine("========== ASSIGN TENANT END ==========");
        return NoContent();
    }
}