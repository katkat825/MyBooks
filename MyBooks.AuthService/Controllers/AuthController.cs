using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Dtos;
using MyBooks.AuthService.Models;
using MyBooks.Common.Services;
using System.Security.Claims;
using System.Text.Json;

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

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchUser(int id, [FromBody] Dictionary<string, object> updates)
        {
            Console.WriteLine($"🛠 Received PATCH request for User ID: {id}");
            Console.WriteLine($"📥 Payload: {System.Text.Json.JsonSerializer.Serialize(updates)}");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                Console.WriteLine("❌ User not found.");
                return NotFound("User not found.");
            }

            foreach (var key in updates.Keys)
            {
                // ✅ Normalize property names to match model (firstName → FirstName)
                var property = typeof(User).GetProperties()
                    .FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));

                if (property != null && property.Name != "PasswordHash" && property.Name != "CreatedBy" && property.Name != "CreatedDate" && property.Name != "LastModifiedBy" && property.Name != "LastModifiedDate")
                {
                    try
                    {
                        // ✅ Handle nullable types correctly
                        Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                        object newValue = updates[key] is JsonElement jsonElement
                            ? JsonElementToObject(jsonElement, targetType) // Convert JSON correctly
                            : Convert.ChangeType(updates[key], targetType);

                        property.SetValue(user, newValue);

                        // ✅ Force EF to track the change
                        _context.Entry(user).Property(property.Name).IsModified = true;
                        Console.WriteLine($"🔹 Successfully updated {property.Name} to {newValue}");
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

            // ✅ Ensure tracking recognizes changes
            _context.Entry(user).State = EntityState.Modified;

            Console.WriteLine($"✅ Saving user - ID: {user.Id}, New FirstName: {user.FirstName}");

            await _context.SaveChangesAsync();

            // 🔄 Fetch the updated user from the database to confirm changes
            var updatedUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            Console.WriteLine($"🔄 Confirmed DB Update - New FirstName: {updatedUser.FirstName}");

            return Ok(new { message = "User updated successfully", updatedUser });
        }

        // ✅ Converts JSON element values correctly
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
