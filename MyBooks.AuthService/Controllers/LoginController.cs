using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Dtos;
using MyBooks.AuthService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyBooks.AuthService.Controllers
{
    [Route("login")]
    [ApiController]
    public class LoginController : Controller
    {
        private readonly AuthDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginController(AuthDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var email = request.Email.ToLower().Trim();
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid username or password.");

            if (!user.IsActive)
                return Unauthorized("Your account has been deactivated.");

            if (user.TenantId == null || user.TenantId == 0)
                return Unauthorized("User is not assigned to an account.");

            var httpClient = _httpClientFactory.CreateClient();
            var tenantResponse = await httpClient.GetAsync(
                $"https://localhost:5005/api/tenant/by-id/{user.TenantId}");

            if (!tenantResponse.IsSuccessStatusCode)
                return Unauthorized("Account lookup failed.");

            var tenant = await tenantResponse.Content.ReadFromJsonAsync<TenantLookupDto>();
            if (tenant == null || !tenant.IsActive)
                return Unauthorized("Account is deactivated.");

            var token = GenerateJwtToken(user);
            return Ok(new { Token = token });
        }

        public string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("role", user.Role),
                new Claim("AgeCategoryId", user.AgeCategoryId.ToString()),
                new Claim("IsActive", user.IsActive.ToString()),
                new Claim("AcceptedAup", user.AcceptedAup.ToString()),
                new Claim("TenantId", user.TenantId.ToString())
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
    }
}
