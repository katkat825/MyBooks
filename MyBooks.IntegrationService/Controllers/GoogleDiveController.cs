using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.Helpers;
using MyBooks.IntegrationService.Data;
using MyBooks.IntegrationService.Models;

namespace MyBooks.IntegrationService.Controllers;

[ApiController]
[Route("api/googledrive")]
[Authorize] 
public class GoogleDriveController : ControllerBase
{
    private readonly IntegrationDbContext _context;

    public GoogleDriveController(IntegrationDbContext context)
    {
        _context = context;
    }

    // GET: api/googledrive
    [HttpGet]
    public async Task<IActionResult> GetConfig()
    {
        var integration = await _context.Integrations
            .FirstOrDefaultAsync(i => i.Provider == StorageProvider.GoogleDrive && i.IsActive);

        if (integration == null)
            return NotFound("No active Google Drive integration for this tenant.");

        var config = integration.ConfigJson.DeserializeConfig<GoogleDriveConfig>();
        return Ok(config);
    }

    // PUT: api/googledrive
    [HttpPut]
    public async Task<IActionResult> UpdateConfig([FromBody] GoogleDriveConfig config)
    {
        var integration = await _context.Integrations
            .FirstOrDefaultAsync(i => i.Provider == StorageProvider.GoogleDrive);

        if (integration == null)
        {
            integration = new Integration
            {
                Provider = StorageProvider.GoogleDrive,
                ConfigJson = config.SerializeConfig(),
                IsActive = true
            };
            _context.Integrations.Add(integration);
        }
        else
        {
            integration.ConfigJson = config.SerializeConfig();
            integration.IsActive = true;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // soft-delete
    [HttpDelete]
    public async Task<IActionResult> RemoveIntegration()
    {
        var integration = await _context.Integrations
            .FirstOrDefaultAsync(i => i.Provider == StorageProvider.GoogleDrive);

        if (integration == null)
            return NotFound();

        integration.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
