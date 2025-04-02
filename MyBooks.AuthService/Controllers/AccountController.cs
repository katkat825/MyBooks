using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Dtos;
using MyBooks.Common.Services;
using System.Security.Claims;

namespace MyBooks.AuthService.Controllers
{
    [Route("api/account")]
    [ApiController]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly HtmlSanitizationService _sanitizationService;

        public AccountController(AuthDbContext context, HtmlSanitizationService sanitizationService)
        {
            _context = context;
            _sanitizationService = sanitizationService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }

        [HttpPatch("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            // sanitize all text fields
            if (!string.IsNullOrEmpty(dto.FirstName))
                user.FirstName = _sanitizationService.Sanitize(dto.FirstName);
            if (!string.IsNullOrEmpty(dto.LastName))
                user.LastName = _sanitizationService.Sanitize(dto.LastName);
            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = _sanitizationService.Sanitize(dto.Email, true);
            if (!string.IsNullOrEmpty(dto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            if(dto.AcceptedAup && !user.AcceptedAup)
            {
                Console.WriteLine("dto.acceptedaup and !user.acceptedaup");
                user.AcceptedAup = true;
                user.LastAcceptedAup = DateTime.UtcNow;
                _context.Entry(user).Property(u => u.AcceptedAup).IsModified = true;
                _context.Entry(user).Property(u => u.LastAcceptedAup).IsModified = true;
            }

            Console.WriteLine("user.accepteaup = ", user.AcceptedAup.ToString());
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully." });
        }
    }
}