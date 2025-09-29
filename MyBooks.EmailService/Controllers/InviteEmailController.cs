using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;

namespace MyBooks.EmailService.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles =AppRoles.AuthService)]
public class InviteEmailController : ControllerBase
{
    private readonly EmailSenderService _emailSender;
    private readonly string _baseUrl;

    public InviteEmailController(EmailSenderService emailSender, IConfiguration config)
    {
        _emailSender = emailSender;
        _baseUrl = config["BaseUrl"];
    }

    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteDto dto)
    {
        try
        {
            var subject = $@"Invitation to {dto.InvitedBy}'s My Book Catalog";
            var body = $@"Hello,

                You’ve been invited to join {dto.InvitedBy}'s My Book Catalog account.  
                To get started, please complete your account setup by clicking the link below:

                {_baseUrl}/invite/{dto.InvitationToken}

                This link will expire in 14 days.  
                If it expires, you can request a new invitation from the login page.

                We’re glad to have you join!
                ";

            await _emailSender.SendEmailAsync(dto.ToEmail, subject, body);
            return Ok();
        }
        catch
        {
            return BadRequest("Email failed to send");
        }
    }

    [HttpPost("password-reset")]
    public async Task<IActionResult> PasswordReset([FromBody] PwdResetDto dto)
    {
        try
        {
            var subject = "My Book Catalog Password Reset";
            var body = $@"Hello,
        
            You requested to set a new password. Please click the link below to set a new password:
            
            {_baseUrl}/reset/{dto.InvitationToken}
            
            This link will expire in 14 days.
            If it expires, you can request a new invitation from the login page.
            
            If you did not submit this request, please reach out to support@mybookcatalog.com";

            await _emailSender.SendEmailAsync(dto.ToEmail, subject, body);
            return Ok();
        }
        catch
        {
            return BadRequest("Email failed to send");
        }
    }
}