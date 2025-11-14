using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MyBooks.AuthService.Controllers;

[ApiController]
[Route("system/token")]
public class SystemTokenController : ControllerBase
{
    private readonly IConfiguration _config;

    public SystemTokenController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost]
    public IActionResult GetSystemToken(
        [FromHeader(Name = "X-Service-Name")] string serviceName,
        [FromHeader(Name = "X-Service-Secret")] string serviceSecret)
    {
        if (string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(serviceSecret))
            return BadRequest("Missing service name or secret.");

        var validSecret = _config[$"ServiceSecrets:{serviceName}"];
        if (validSecret == null || validSecret != serviceSecret)
        {
            return Unauthorized();
        }

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, serviceName),
            new Claim("role", serviceName) // match LoginController style
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5), // short-lived for system tokens
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { token = tokenString });
    }
}
