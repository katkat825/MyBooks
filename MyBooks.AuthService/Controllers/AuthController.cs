using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Dtos;
using MyBooks.AuthService.Models;
using MyBooks.Common.Services;

namespace MyBooks.AuthService.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _config;
        private readonly HtmlSanitizationService _sanitizationService;

        public AuthController(AuthDbContext context, IConfiguration config, HtmlSanitizationService sanitizationService)
        {
            _context = context;
            _config = config;
            _sanitizationService = sanitizationService;
        }

        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(UserDto request)
        {
            //sanitize inputs
            request.FirstName = _sanitizationService.Sanitize(request.FirstName);
            request.LastName = _sanitizationService.Sanitize(request.LastName);
            request.Email = _sanitizationService.Sanitize(request.Email);

            //check if email in use
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))  return BadRequest("Email already in use.");

            var ageCategoryExists = await VerifyAgeCategoryExists(request.AgeCategoryId);
            if (!ageCategoryExists) return BadRequest("Invalid AgeCategoryId.");

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Role = request.Role,
                AgeCategoryId = request.AgeCategoryId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User resgistered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var user = await _context.Users
                .Where(u => u.Email.ToLower().Trim() == request.Email.ToLower().Trim())
                .FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid username or password.");

            var token = GenerateJwtToken(user);
            return Ok(new {Token  = token});
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> VerifyAgeCategoryExists(int ageCategoryId)
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("https://localhost:5001/api/books/agecategories");

            if (!response.IsSuccessStatusCode) return false;

            var ageCategories = await response.Content.ReadFromJsonAsync<List<AgeCategoryDto>>();
            return ageCategories.Any(a => a.Id == ageCategoryId);
        }
    }
}
