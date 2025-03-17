using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Dtos;
using MyBooks.AuthService.Models;
using MyBooks.Common.Services;
using System.Security.Claims;

namespace MyBooks.AuthService.Controllers
{
    [Route("api/users")]
    [ApiController]   
    [Authorize(Roles = "Admin")] 
    public class AuthController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly HtmlSanitizationService _sanitizationService;

        public AuthController(AuthDbContext context, IConfiguration config, HtmlSanitizationService sanitizationService)
        {
            _context = context;
            _sanitizationService = sanitizationService;
        }

        [HttpGet]        
        public async Task<IActionResult> GetUsers()
        {
            Console.WriteLine($"User authenticated? {User.Identity.IsAuthenticated}");

            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim type: {claim.Type}, value: {claim.Value}");
            }
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var manualRole = User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? "None";
            Console.WriteLine($"User role from jwt: {userRole}, Manual role: {manualRole}");


            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            Console.WriteLine($"🔹 Recognized Roles: {string.Join(", ", roles)}");

            if (!User.IsInRole("Admin"))
            {
                Console.WriteLine("User not recognized as an admin");
            }
            else
            {
                Console.WriteLine("user is recognized as an admin");
            }
                            
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, [FromBody] UserDto request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            //sanitize input
            request.FirstName = _sanitizationService.Sanitize(request.FirstName);
            request.LastName = _sanitizationService.Sanitize(request.LastName);
            request.Email = _sanitizationService.Sanitize(request.Email);

            var ageCategoryExists = await VerifyAgeCategoryExists(request.AgeCategoryId);
            if (!ageCategoryExists) return BadRequest("Invalid age category id.");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.Role = request.Role;
            user.AgeCategoryId = request.AgeCategoryId;

            if (!string.IsNullOrWhiteSpace(request.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok("User updated successfully.");
        }

        [HttpPost("register")]
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
