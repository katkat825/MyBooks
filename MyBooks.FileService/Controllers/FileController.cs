using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.Services;
using MyBooks.Common.Dtos;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Validators;
using MyBooks.FileService.Services;
using System.Text.RegularExpressions;
using MyBooks.Common.BaseClasses;

namespace MyBooks.FileService.Controllers;

[Route("")]
[ApiController]
[Authorize]
public class FileController : ControllerBase
{
    private readonly FileDbContext _context;
    private readonly HtmlSanitizationService _sanitizationService;
    private readonly GoogleDriveClient _googleDriveClient;
    private readonly CloudflareR2Client _r2Client;
    private readonly ClamAvScanService _clamAv;

    public FileController(
        FileDbContext context,
        HtmlSanitizationService sanitizationService,
        GoogleDriveClient googleDriveClient,
        CloudflareR2Client r2Client,
        ClamAvScanService clamAv)
    {
        _context = context;
        _sanitizationService = sanitizationService;
        _googleDriveClient = googleDriveClient;
        _r2Client = r2Client;
        _clamAv = clamAv;
    }

    // upload File - only owner or superadmin
    [HttpPost("")]
    [HttpPost("/")]
    [Authorize(Roles = AppRoles.OwnerPlus)]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] int bookId, [FromForm] string bookTitle, [FromForm] string? folderId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var tenantId = _context.GetCurrentTenantId();

        var integration = await _context.GoogleIntegrations
            .FirstOrDefaultAsync(g => g.TenantId == tenantId && g.IsActive);
        if (integration == null)
            return BadRequest("Google Drive not configured for this tenant.");
        
        if (string.IsNullOrWhiteSpace(folderId))
            folderId = await _googleDriveClient.GetOrCreateFolderAsync("MyBookCatalog", "root", integration.RefreshToken);

        // basic validation check (size, extension, MIME, signature)
        var fileValidator = new FileValidator();
        var fileValidationResult = fileValidator.Validate(file);
        if (!fileValidationResult.IsValid)
            return BadRequest(fileValidationResult.Errors);

        // structural validation (pdf or epub integrity)
        var structureValidator = new FileValidationService();
        await using var structureStream = file.OpenReadStream();
        var structureResult = await structureValidator.ValidateAsync(structureStream, file.FileName);
        if(!structureResult.IsValid)
            return BadRequest(structureResult.ErrorMessage);
        
        // malware scan (ClamAV)
        await using var scanStream = file.OpenReadStream();
        var isClean = await _clamAv.IsFileCleanAsync(scanStream);
        if (!isClean)
            return BadRequest("Malware detected in uploaded file.");
            
        var extension = Path.GetExtension(file.FileName);
        var sanitizedBookTitle = Regex.Replace(_sanitizationService.Sanitize(bookTitle).Trim(), @"\s+", "_");

        var fileNameWithoutExt = $"{bookId}_{sanitizedBookTitle}";
        var fileName = $"{fileNameWithoutExt}{extension}";

        // deactivate old file if exists
        var existingFile = await _context.Files.FirstOrDefaultAsync(f => f.BookId == bookId && f.IsActive);
        if (string.IsNullOrEmpty(folderId) && existingFile?.FolderId != null)
            folderId = existingFile.FolderId;
        if (existingFile != null)
            {
                if (existingFile.GoogleIntegrationId != null)
                    await _googleDriveClient.DeleteFileAsync(existingFile.FilePath, integration.RefreshToken);
                existingFile.IsActive = false;
                _context.Files.Update(existingFile);
                await _context.SaveChangesAsync();
            }

        // upload to Google Drive
        string fileId;
        using (var stream = file.OpenReadStream())
        {
            try
            {
                fileId = await _googleDriveClient.UploadFileAsync(
                    fileName, stream, file.ContentType,
                    folderId, integration.RefreshToken);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var fallbackFolderId = await _googleDriveClient.GetOrCreateFolderAsync(
                    "MyBookCatalog", "root", integration.RefreshToken
                );

                // create new stream to prevent potential issues
                using (var retryStream = file.OpenReadStream())
                {
                    fileId = await _googleDriveClient.UploadFileAsync(
                        fileName, retryStream, file.ContentType,
                        fallbackFolderId, integration.RefreshToken
                    );
                }

                folderId = fallbackFolderId;                    
            }
        }

        var fileMetadata = new FileMetadata
        {
            FileName = fileName,
            FilePath = fileId, // Google Drive fileId
            ContentType = file.ContentType,
            FileSize = file.Length,
            BookId = bookId,
            IsActive = true,
            GoogleIntegrationId = integration.Id,
            FolderId = folderId
        };

        var validator = new FileMetaValidator();
        var validationResult = validator.Validate(fileMetadata);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        _context.Files.Add(fileMetadata);
        await _context.SaveChangesAsync();

        return Ok(new { FileId = fileMetadata.Id, Message = "File uploaded successfully" });
    }

    // file download or read inline
    [HttpGet("{id}")]
    public async Task<IActionResult> DownloadFile(int id, [FromQuery] bool inline = false)
    {
        var file = await _context.Files
            .Include(f => f.GoogleIntegration)
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

        if (file == null)
            return NotFound("File not found.");

        Stream stream;
        string contentType = file.ContentType;
        string path = file.FilePath;

        try
        {
            if ((!string.IsNullOrEmpty(file.ConvertedFilePath) && inline) || file.StorageSource == StorageSource.MyBookCatalog)
            {
                if (inline && file.IsConverted == true)
                {
                    path = file.ConvertedFilePath;
                    contentType = "application/epub+zip";
                }
                stream = await _r2Client.GetFileStreamAsync(path);
            }
            else
            {
                stream = await _googleDriveClient.GetFileStreamAsync(
                    file.FilePath, file.GoogleIntegration.RefreshToken);
            }

            if (inline)
                return File(stream, contentType);
            return File(stream, file.ContentType, file.FileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileController] Error during file streaming: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    // soft-delete file metadata - only owner or superadmin
    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.OwnerPlus)]
    public async Task<IActionResult> DeleteFile(int id)
    {
        var file = await _context.Files
            .Include(f => f.GoogleIntegration)
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

        if (file == null)
            return NotFound("File not found.");

        // delete converted file from mybookcatalog storage if exists
        if (!string.IsNullOrEmpty(file.ConvertedFilePath))
        {
            var deleted = await _r2Client.DeleteFileAsync(file.ConvertedFilePath);
            Console.WriteLine($"[FileController] Delete R2 file result: {deleted}");
        }
        else
        {
            Console.WriteLine("no file.ConvertedFilePath value - R2 deletion skipped");
        }

        file.IsActive = false;
        _context.Files.Update(file);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // get file for single book
    [HttpGet("book/{bookId}")]
    public async Task<IActionResult> GetFilesByBook(int bookId)
    {
        var files = await _context.Files
            .Where(f => f.BookId == bookId && f.IsActive)
            .ToListAsync();

        return Ok(files);
    }

    // get file metadata for a given file id
    [HttpGet("metadata/{id}")]
    public async Task<IActionResult> GetFileMetadata(int id)
    {
        var file = await _context.Files.FindAsync(id);
        if (file == null)
            return NotFound("File not found.");
        return Ok(file);
    }

    [HttpGet("ids/integration/{integrationId}")]
    [Authorize(Roles = AppRoles.OwnerPlus)]
    public async Task<IActionResult> GetAllFileIds(int integrationId)
    {
        var tenantId = _context.GetCurrentTenantId();

        // return only active Google Drive file IDs for this integration
        var fileIds = await _context.Files
            .Where(f => f.TenantId == tenantId && f.IsActive && f.FilePath != null && f.GoogleIntegrationId == integrationId)
            .Select(f => f.FilePath!)
            .ToListAsync();

        return Ok(fileIds);
    }
}