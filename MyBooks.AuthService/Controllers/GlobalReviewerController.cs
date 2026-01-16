using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Models;
using MyBooks.Common.BaseClasses;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace MyBooks.AuthService.Controllers;

[ApiController]
[Route("support/reviewers")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class GlobalReviewerController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _config;

    public GlobalReviewerController(AuthDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GlobalReviewerAccess>>> GetAllReviewers()
    {
        var reviewers = await _context.GlobalReviewerAccess
            .Include(g => g.User)
            .AsNoTracking()
            .ToListAsync();

        return Ok(reviewers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GlobalReviewerAccess>> GetByUserId(int id)
    {
        var access = await _context.GlobalReviewerAccess
            .Include(g => g.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.UserId == id);

        if (access == null)
            return NotFound();

        return Ok(access);
    }

    [HttpPost("{id}")]
    public async Task<IActionResult> GrantReviewerAccess(int id)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound($"User {id} not found.");

        // check if already has access
        var existingAccess = await _context.GlobalReviewerAccess
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.UserId == id);

        if (existingAccess == null)
        {
            var access = new GlobalReviewerAccess
            {
                UserId = id,
                IsActive = true
            };

            _context.GlobalReviewerAccess.Add(access);
            await _context.SaveChangesAsync();

            var reviewerEmail = user.Email + "-9999";

            var reviewerClone = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == reviewerEmail && u.TenantId == 9999);

            if (reviewerClone != null)
            {
                reviewerClone.FirstName = user.FirstName;
                reviewerClone.LastName = user.LastName;
                reviewerClone.IsActive = true;
                reviewerClone.IsVisible = true;
                reviewerClone.Role = AppRoles.GlobalReviewer;
                reviewerClone.TenantId = 9999;
                _context.Entry(reviewerClone).State = EntityState.Modified;
                await _context.SaveChangesAsSuperadminAsync();
            }
            else
            {
                reviewerClone = new User
                {
                    TenantId = 9999,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = reviewerEmail,
                    Role = AppRoles.GlobalReviewer,
                    IsActive = true,
                    IsVisible = true,
                    AgeCategoryId = 3,
                    AcceptedAup = true,
                    LastAcceptedAup = DateTime.UtcNow,
                    PasswordHash = "SYSTEM_CLONE_NO_LOGIN"
                };
                _context.Users.Add(reviewerClone);
                await _context.SaveChangesAsSuperadminAsync();
            }
        }
        else
        {
            existingAccess.IsActive = true;
            _context.Entry(existingAccess).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Reviewer access granted for user {id}." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeAccess(int id)
    {
        var access = await _context.GlobalReviewerAccess
            .FirstOrDefaultAsync(r => r.UserId == id);

        if (access == null)
            return NotFound();

        access.IsActive = false;
        _context.Entry(access).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        var sourceUser = await _context.Users
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(u => u.Id == id);

        var reviewerEmail = sourceUser.Email + "-9999";

        if (sourceUser != null)
        {
            var reviewerClone = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == reviewerEmail && u.TenantId == 9999);

            if (reviewerClone != null)
            {
                reviewerClone.IsActive = false;
                reviewerClone.IsVisible = false;
                _context.Entry(reviewerClone).State = EntityState.Modified;
                await _context.SaveChangesAsSuperadminAsync();
            }
        }
        return Ok(new { message = $"Reviewer access revoked for user {id}." });
    }

    [HttpPost("switch")]
    [Authorize]
    public async Task<IActionResult> SwitchToReviewerPortal()
    {
        // get the current authenticated user (from their normal tenant)
        var userIdString = _context.GetCurrentUserId();
        if (!int.TryParse(userIdString, out var userId))
            return Unauthorized("Invalid user identity.");

        // verify they have active GlobalReviewerAccess
        var access = await _context.GlobalReviewerAccess
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.UserId == userId && g.IsActive);

        if (access == null)
            return Forbid("Reviewer access not granted or inactive.");

        // find their reviewer clone in tenant 9999
        var sourceUser = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (sourceUser == null)
            return NotFound("Source user not found.");

        var reviewerEmail = sourceUser.Email + "-9999";

        var reviewerClone = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == reviewerEmail && u.TenantId == 9999 && u.IsActive);

        if (reviewerClone == null)
            return NotFound("Active reviewer clone not found in tenant 9999.");

        // create JWT token for the reviewer clone
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, reviewerClone.Id.ToString()),
            new Claim(ClaimTypes.Email, reviewerClone.Email),
            new Claim("role", AppRoles.GlobalReviewer),
            new Claim("TenantId", "9999"),
            new Claim("AgeCategoryId", "3"),
            new Claim("IsActive", reviewerClone.IsActive.ToString()),
            new Claim("AcceptedAup", reviewerClone.AcceptedAup.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(12), 
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            Token = tokenString,
            ReviewerUserId = reviewerClone.Id,
            ReviewerEmail = reviewerClone.Email,
            ReviewerTenantId = reviewerClone.TenantId
        });
    }
}
