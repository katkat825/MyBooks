using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Dtos;
using MyBooks.AuthService.Models;
using MyBooks.Common.Services;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using System.Security.Claims;
using System.Text.Json;
using System.Linq;
using MyBooks.AuthService.Services;

namespace MyBooks.AuthService.Controllers;

[Route("api/users")]
[ApiController]
[Authorize(Roles = AppRoles.OwnerPlus)]
public class AuthController : Controller
{
    private readonly AuthDbContext _context;
    private readonly HtmlSanitizationService _sanitizationService;
    private readonly InvitationService _invitationService;

    public AuthController(AuthDbContext context, IConfiguration config, HtmlSanitizationService sanitizationService, InvitationService invitationService)
    {
        _context = context;
        _sanitizationService = sanitizationService;
        _invitationService = invitationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    [HttpGet("all-users")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role != AppRoles.Support)
            .ToArrayAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound("User not found.");

        return Ok(user);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchUser(int id, [FromBody] Dictionary<string, object> updates)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        foreach (var key in updates.Keys)
        {
            var property = typeof(User).GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));

            if (property != null && property.Name != "PasswordHash" && property.Name != "CreatedBy" && property.Name != "CreatedDate" && property.Name != "LastModifiedBy" && property.Name != "LastModifiedDate")
            {
                try
                {
                    Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    object newValue = updates[key] is JsonElement jsonElement
                        ? JsonElementToObject(jsonElement, targetType)
                        : Convert.ChangeType(updates[key], targetType);

                    if (string.Equals(property.Name, nameof(user.Role), StringComparison.OrdinalIgnoreCase))
                    {
                        var newRole = newValue?.ToString();

                        bool touchesPrivileged =
                            user.Role == AppRoles.SuperAdmin || user.Role == AppRoles.Owner ||
                            newRole == AppRoles.SuperAdmin || newRole == AppRoles.Owner;

                        if (touchesPrivileged && !User.IsInRole(AppRoles.SuperAdmin))
                        {
                            return Forbid("You are not authorized to change this user's role.");
                        }
                    }

                    property.SetValue(user, newValue);

                    _context.Entry(user).Property(property.Name).IsModified = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to update {property.Name}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"⚠ Skipping invalid field: {key}");
            }
        }

        _context.Entry(user).State = EntityState.Modified;

        await _context.SaveChangesAsync();

        var updatedUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

        return Ok(new { message = "User updated successfully", updatedUser });
    }

    private static object JsonElementToObject(JsonElement element, Type targetType)
    {
        try
        {
            return targetType == typeof(int) ? element.GetInt32() :
                    targetType == typeof(string) ? element.GetString() :
                    targetType == typeof(bool) ? element.GetBoolean() :
                    targetType == typeof(double) ? element.GetDouble() :
                    targetType == typeof(DateTime) ? element.GetDateTime() :
                    Convert.ChangeType(element.ToString(), targetType);
        }
        catch
        {
            return null;
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserDto request)
    {
        //sanitize inputs
        request.FirstName = _sanitizationService.Sanitize(request.FirstName);
        request.LastName = _sanitizationService.Sanitize(request.LastName);
        request.Email = _sanitizationService.Sanitize(request.Email, true);

        var existingUser = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == _context.GetCurrentTenantId());

        if (existingUser != null)
        {
            if (!existingUser.IsVisible)
            {
                existingUser.IsVisible = true;
                existingUser.IsActive = true;
                existingUser.FirstName = request.FirstName;
                existingUser.LastName = request.LastName;
                existingUser.AgeCategoryId = request.AgeCategoryId;
                existingUser.Role = request.Role;
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                var reinvite = await _invitationService.CreateAndSendInviteAsync(existingUser.Id);

                return Ok();
            }

            return BadRequest("Email already in use");
        }

        var requestedRole = request.Role;

        if (!AppRoles.AllRoles.Contains(requestedRole)) return BadRequest("Invalid role.");

        if (!AppRoles.AssignableRoles.Contains(requestedRole) && !User.IsInRole(AppRoles.SuperAdmin))
            return Forbid("You are not authorized to assign this role.");

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Role = request.Role,
            AgeCategoryId = request.AgeCategoryId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            AcceptedAup = false
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var invite = await _invitationService.CreateAndSendInviteAsync(user.Id);

        return Ok();
    }

    [HttpPatch("deactivate/{id}")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");

        if (user.Role == AppRoles.SuperAdmin || user.Role == AppRoles.Owner)
            return Forbid("Cannot deactivate a MyBookCatalog Support user or Owner user.");

        user.IsActive = false;
        _context.Entry(user).Property(u => u.IsActive).IsModified = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "User deactivated successfully" });
    }

    [HttpPatch("reactivate/{id}")]
    public async Task<IActionResult> ReactivateUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");

        user.IsActive = true;
        _context.Entry(user).Property(u => u.IsActive).IsModified = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "User reactivated successfully" });
    }

    [HttpPatch("delete/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");

        if (user.Role == AppRoles.SuperAdmin || user.Role == AppRoles.Owner)
            return Forbid("Cannot delete a MyBookCatalog Support user or Owner user.");

        user.IsVisible = false;
        _context.Entry(user).Property(u => u.IsVisible).IsModified = true;

        await _context.SaveChangesAsync();

        return Ok(new { message = "User deleted successfully" });
    }

    // feedback since unique email is enforced across tenants
    [HttpGet("check-email")]
    [AllowAnonymous] 
    public async Task<IActionResult> CheckEmail([FromQuery] string email, [FromQuery] int? excludeUserId = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { exists = false, message = "Email is required." });

        // sanitize the email input
        email = _sanitizationService.Sanitize(email, true);

        // check globally (ignore tenant filters)
        var exists = await _context.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Email == email && u.Id != excludeUserId);

        return Ok(new { exists });
    }
}