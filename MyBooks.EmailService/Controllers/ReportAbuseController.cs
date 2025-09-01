using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyBooks.EmailService.Models;
using MyBooks.EmailService.Services;
using System.Security.Claims;

namespace MyBooks.EmailService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportAbuseController : ControllerBase
    {
        private readonly EmailSenderService _emailSender;

        public ReportAbuseController(EmailSenderService emailSender)
        {
            _emailSender = emailSender;
        }

        [HttpPost]
        [Authorize] 
        public async Task<IActionResult> Report([FromBody] ReportAbuseDto dto)
        {
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
            Console.WriteLine($"[ReportAbuse] Authorization header: {authHeader}");
            
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tenantId = User.FindFirst("TenantId")?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            var body = $@"Abuse Report Submitted:

                Page URL: {dto.PageUrl}
                Description: {dto.Description}

                Submitted By:
                UserId: {userId}
                TenantId: {tenantId}
                User Email: {userEmail}

                Contact Email (optional): {dto.ContactEmail ?? "N/A"}
                ";

            await _emailSender.SendEmailAsync("abuse@mybookcatalog.com", "Report Abuse", body);

            return Ok(new { success = true });
        }
    }
}
