using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Services;
using MyBooks.Common.BaseClasses;
using System.Formats.Asn1;

namespace MyBooks.FileService.Controllers
{
    [ApiController]
    [Route("api/google-integrations")]
    [Authorize(Roles = AppRoles.OwnerPlus)]
    public class GoogleIntegrationController : ControllerBase
    {
        private readonly FileDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GoogleDriveClient _googleDriveClient;

        public GoogleIntegrationController(FileDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory, GoogleDriveClient googleDriveClient)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _googleDriveClient = googleDriveClient;
        }

        // STEP 1: Get OAuth consent URL
        [HttpGet("authorize-url")]
        public IActionResult GetAuthorizationUrl()
        {
            var tenantId = _context.GetCurrentTenantId();
            var userId = _context.GetCurrentUserId();
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var nonce = Guid.NewGuid().ToString("N");

            var state = $"{tenantId}:{nonce}:{userId}:{today}";

            var clientId = _config["GoogleOAuth:ClientId"];
            var redirectUri = _config["GoogleOAuth:RedirectUriLocal"]; // must match in Google Cloud console
            var scopes = "https://www.googleapis.com/auth/drive.file https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/drive.metadata.readonly";

            var url = $"https://accounts.google.com/o/oauth2/v2/auth" +
                      $"?client_id={clientId}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&state={state}" +
                      $"&response_type=code" +
                      $"&access_type=offline" +
                      $"&prompt=consent" +
                      $"&scope={Uri.EscapeDataString(scopes)}";

            return Ok(new { Url = url });
        }

        // STEP 2: Callback from Google
        [HttpGet("callback")]
        [AllowAnonymous] // Google will hit this endpoint directly
        public async Task<IActionResult> OAuthCallback([FromQuery] string code, [FromQuery] string state)
        {
            var tenantId = 0;
            var userId = "";
            var date = "";

            if (!string.IsNullOrEmpty(state))
            {
                var parts = state.Split(":");
                if (parts.Length == 4)
                {
                    int.TryParse(parts[0], out tenantId);
                    // parts[1] is random GUID - ignore for MVP
                    userId = parts[2];
                    date = parts[3];
                }

                if (date != DateTime.UtcNow.ToString("yyyyMMdd"))
                    return BadRequest("State expired");
            }

            var clientId = _config["GoogleOAuth:ClientId"];
            var clientSecret = _config["GoogleOAuth:ClientSecret"];
            var redirectUri = _config["GoogleOAuth:RedirectUriLocal"];

            var http = _httpClientFactory.CreateClient();

            var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"code", code},
                {"client_id", clientId},
                {"client_secret", clientSecret},
                {"redirect_uri", redirectUri},
                {"grant_type", "authorization_code"}
            }));

            if (!response.IsSuccessStatusCode)
                return BadRequest("Google token exchange failed");

            var json = await response.Content.ReadAsStringAsync();
            dynamic tokenObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json)!;

            string refreshToken = tokenObj.refresh_token;
            string accessToken = tokenObj.access_token;
            DateTime accessExpiry = DateTime.UtcNow.AddSeconds((int)tokenObj.expires_in);

            // also call Google UserInfo API to get account email
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            var userInfoResp = await http.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            var userInfoJson = await userInfoResp.Content.ReadAsStringAsync();
            dynamic userInfo = Newtonsoft.Json.JsonConvert.DeserializeObject(userInfoJson)!;
            string email = userInfo.email;

            // save integration
            var integration = new GoogleIntegration
            {
                TenantId = tenantId,
                AccountEmail = email,
                RefreshToken = refreshToken,
                AccessToken = accessToken,
                AccessTokenExpiry = accessExpiry,
                IsActive = true,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.GoogleIntegrations.Add(integration);
            await _context.SaveChangesAsSystemAsync();

            // redirect user back to UI
            return Redirect(_config["GoogleOAuth:PostLoginRedirect"]);
        }

        // STEP 3: List integrations
        [HttpGet]
        public async Task<IActionResult> GetIntegrations()
        {            
            var tenantId = _context.GetCurrentTenantId();
            var list = await _context.GoogleIntegrations
                .Where(g => g.TenantId == tenantId && g.IsActive)
                .ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}/folders")]
        public async Task<IActionResult> GetFolders([FromQuery] string? parentId = "root")
        {
            var tenantId = _context.GetCurrentTenantId();
            var integration = await _context.GoogleIntegrations
                .FirstOrDefaultAsync(g => g.TenantId == tenantId && g.IsActive);
            if (integration == null)
                return BadRequest("Google Drive not configured for this account.");


            var files = await _googleDriveClient.ListFoldersAsync(parentId ?? "root", integration.RefreshToken);
            
            var selectedIds = integration.DriveFolderIds ?? new List<string>();

            var folders = files.Select(f => new
            {
                Id = f.Id,
                Name = f.Name,
                IsSelected = selectedIds.Contains(f.Id)
            }).ToList();

            //debugging 
            Console.WriteLine($"[Controller] Returning {folders.Count} folders to client.");

            return Ok(folders);
        }

        [HttpPut("{id}/folders")]
        public async Task<IActionResult> UpdateFolders(int id, [FromBody] List<string> folderIds)
        {
            var tenantId = _context.GetCurrentTenantId();
            var userId = _context.GetCurrentUserId();

            var integration = await _context.GoogleIntegrations
                .FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId && g.IsActive);

            if (integration == null)
                return NotFound("Integration not found.");

            // update folder list
            integration.DriveFolderIds = folderIds ?? new List<string>();

            _context.GoogleIntegrations.Update(integration);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                IntegrationId = integration.Id,
                FolderIds = integration.DriveFolderIds
            });
        }

        // optional: deactivate
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIntegration(int id)
        {
            var integration = await _context.GoogleIntegrations.FindAsync(id);
            if (integration == null) return NotFound();

            integration.IsActive = false;
            _context.GoogleIntegrations.Update(integration);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
