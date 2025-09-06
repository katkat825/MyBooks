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

        _context.Users.Add(user);
        await _context.SaveChangesAsSystemAsync();

        return Ok(new { userId = user.Id, created = true });
    }

    [HttpPost("assign-tenant")]
    public async Task<IActionResult> AssignTenant(AssignTenantDto request)
    {
        var user = await _context.Users.FindAsync(request.UserId);

        if (user == null) return NotFound();
        if (user.TenantId != null) return BadRequest($"User already assigned to tenant: {user.TenantId}");

        user.TenantId = request.TenantId;
        _context.Update(user);
        await _context.SaveChangesAsSystemAsync();

        return NoContent();
    }
}