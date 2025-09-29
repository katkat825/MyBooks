using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Models;
using MyBooks.AuthService.Services;

namespace MyBooks.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly InvitationService _invitationService;

    public InvitationController(AuthDbContext context, InvitationService invitationService)
    {
        _context = context;
        _invitationService = invitationService;
    }

    [AllowAnonymous]
    [HttpPost("validate")]    
    public async Task<IActionResult> Validate([FromBody] string token)
    {
        Console.WriteLine($"[Validate] Raw token from request: '{token}'");

        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("[Validate] Token was null or whitespace.");
            return BadRequest("Invitation is not valid.");
        }

        var invite = await _context.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.InvitationToken == token);

        if (invite == null || !invite.IsActive)
        {
            Console.WriteLine($"[Validate] No invite found for token: '{token}' or token is null");
            return BadRequest("Invitation is not valid.");
        }

        if (invite.ExpirationDate < DateTime.UtcNow)
        {
            invite.IsActive = false;
            invite.DeactivationReason = InvitationDeactivationReason.Expired;
            _context.Entry(invite).State = EntityState.Modified;
            await _context.SaveChangesAsSystemAsync();
            Console.WriteLine($"[Validate] Invite expired. Id={invite.Id}, Expiration={invite.ExpirationDate}");
            return BadRequest("Invitation has expired.");
        }

        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == invite.UserId);
        if (user == null || !user.IsActive)
        {
            Console.WriteLine($"[Validate] Associated user not active or not found. UserId={invite.UserId}");
            return BadRequest("Associated user is not active.");
        }

        return Ok(new { invite.Email, user.FirstName, user.LastName });
    }

    [AllowAnonymous]
    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteInvitationDto dto)
    {
        var invite = await _context.Invitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.InvitationToken == dto.Token);

        if (invite == null || !invite.IsActive)
            return BadRequest("Invitation is not valid.");

        if (invite.ExpirationDate < DateTime.UtcNow)
        {
            invite.IsActive = false;
            invite.DeactivationReason = InvitationDeactivationReason.Expired;
            _context.Entry(invite).State = EntityState.Modified;
            await _context.SaveChangesAsSystemAsync();
            return BadRequest("Invitation has expired.");
        }

        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == invite.UserId);
        if (user == null)
            return NotFound("User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        _context.Entry(user).State = EntityState.Modified;

        invite.IsActive = false;
        invite.DeactivationReason = InvitationDeactivationReason.Used;
        _context.Entry(invite).State = EntityState.Modified;

        await _context.SaveChangesAsSystemAsync();

        return Ok(new { message = "Invitation completed. Password set." });
    }

    [AllowAnonymous]
    [HttpPost("resend")]
    public async Task<IActionResult> Resend([FromBody] string email)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !user.IsActive)
            return NotFound("User not found or inactive.");

        var invite = await _invitationService.CreateAndSendPasswordResetAsync(user.Id);

        if (invite == null)
            return StatusCode(500, "Failed to send password reset email.");

        return Ok(new {message = "Password reset email sent."});
    }
}

public class CompleteInvitationDto
{
    public string Token { get; set; } = null!;
    public string Password { get; set; } = null!;
}
