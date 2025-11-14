using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Models;
using MyBooks.Common.BaseClasses;
using System.Text.Json;

namespace MyBooks.AuthService.Controllers;

[Route("support/users")]
[ApiController]
[Authorize(Roles = AppRoles.SuperAdmin)]

public class SupportUsersController : ControllerBase
{
    private readonly AuthDbContext _context;

    public SupportUsersController(AuthDbContext context)
    {
        _context = context;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Role != AppRoles.Support)
            .ToArrayAsync();

        return Ok(users);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> SupportPatchUser(int id, [FromBody] Dictionary<string, object> updates)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null || !AppRoles.AllRoles.Contains(user.Role))
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

                        if (!AppRoles.AllRoles.Contains(newRole))
                            return BadRequest("Invalid role");
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

        await _context.SaveChangesAsSuperadminAsync();

        var updatedUser = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        return Ok(new { message = "User updated successfully", updatedUser });
    }

    [HttpPatch("{id}/reactivate")]
    public async Task<IActionResult> SupportReactivateUser(int id)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()  // bypass IsVisible & tenantId filter
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound("User not found.");

        if (!AppRoles.AllRoles.Contains(user.Role))
            return BadRequest("Invalid role.");

        user.IsActive = true;
        user.IsVisible = true; // also restore visibility
        _context.Entry(user).Property(u => u.IsActive).IsModified = true;
        _context.Entry(user).Property(u => u.IsVisible).IsModified = true;

        await _context.SaveChangesAsSuperadminAsync();
        var updatedUser = await _context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        return Ok(new { message = "User reactivated successfully by SuperAdmin", updatedUser });
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
}