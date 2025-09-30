using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Models;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using MyBooks.Common.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace MyBooks.AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class ImpersonationController : ControllerBase
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly SystemTokenHelper _systemTokenHelper;

    public ImpersonationController(AuthDbContext context, IConfiguration config, HttpClient httpClient, SystemTokenHelper systemTokenHelper)
    {
        _context = context;
        _config = config;
        _httpClient = httpClient;
        _systemTokenHelper = systemTokenHelper;
    }

    public int GetCurrentUserIdAsInt()
    {
        var userId = _context.GetCurrentUserId();

        if (int.TryParse(userId, out var id))
            return id;

        throw new InvalidOperationException("Authenticated user id not found or invalid");
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> Impersonate(int userId)
    {
        // get current superadmin (the impersonator)
        var impersonatorId = this.GetCurrentUserIdAsInt();

        // get target user (ignore filters so we can see support users too)
        var targetUser = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (targetUser == null)
            return NotFound("Target user not found");

        if (targetUser.TenantId == null || targetUser.TenantId == 0)
            return BadRequest("Target user is not assigned to a tenant");

        int? logId = null;

        // create impersonation log in SupportService (behind the scenes)
        try
        {
            var systemToken = await _systemTokenHelper.GetSystemTokenAsync("AuthService", _config["ServiceSecrets:AuthService"]);
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", systemToken);
            var supportUrl = _config["ServiceUrls:SupportService"];

            var dto = new ImpersonationDto
            {
                TargetUserId = targetUser.Id,
                ImpersonatingUserId = impersonatorId
            };

            var response = await _httpClient.PostAsJsonAsync($"{supportUrl}/api/ImpersonationLog/start", dto);

            if (response.IsSuccessStatusCode)
            {
                logId = await response.Content.ReadFromJsonAsync<int>();
            }
            else
            {
                Console.WriteLine($"[WARN] Failed to log impersonation start for user {targetUser.Id}: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Exception logging impersonation: {ex.Message}");
        }

        // build claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, targetUser.Id.ToString()),
            new Claim(ClaimTypes.Email, targetUser.Email),
            new Claim("role", targetUser.Role),
            new Claim("TenantId", targetUser.TenantId.ToString()),
            new Claim("AgeCategoryId", targetUser.AgeCategoryId.ToString()),

            // impersonation markers
            new Claim("IsImpersonating", "true"),
            new Claim("ImpersonatorId", impersonatorId.ToString()),
            new Claim("ImpersonationLogId", logId.Value.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddMinutes(60), // short-lived token
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { Token = tokenString, LogId = logId });
    }

    [HttpPost("stop/{logId}")]
    public async Task<IActionResult> Stop(int logId)
    {
        var systemToken = await _systemTokenHelper.GetSystemTokenAsync("AuthService", _config["ServiceSecrets:AuthService"]);
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", systemToken);

        var supportUrl = _config["ServiceUrls:SupportService"];
        var response = await _httpClient.PostAsJsonAsync($"{supportUrl}/api/ImpersonationLog/stop/{logId}", new { });

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, "Failed to stop impersonation");
        }

        return NoContent();
    }
}
