using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.Services;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Validators;
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace MyBooks.FileService.Controllers
{
    [Route("api/files")]
    [ApiController]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly FileDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly string _storagePath;
        private readonly HtmlSanitizationService _sanitizationService;

        public FileController(FileDbContext context, IWebHostEnvironment environment, IConfiguration config, HtmlSanitizationService sanitizationService)
        {
            _context = context;
            _environment = environment;
            _storagePath = config["FileStorage"] ?? Path.Combine(_environment.WebRootPath, "uploads");
            _sanitizationService = sanitizationService;

            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);            
        }

        // upload File
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] int bookId, [FromForm] string bookTitle)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var fileValidator = new FileValidator();
            var fileValidationResult = fileValidator.Validate(file);
            if (!fileValidationResult.IsValid)
            {
                return BadRequest(fileValidationResult.Errors);
            }

            var extension = Path.GetExtension(file.FileName);
            var sanitizedBookTitle = _sanitizationService.Sanitize(bookTitle).Trim();
            sanitizedBookTitle = Regex.Replace(sanitizedBookTitle, @"\s+", "_");
            var fileName = $"{bookId}_{sanitizedBookTitle}{extension}";

            var filePath = Path.Combine(_storagePath, fileName);

            //remove old file if it exists
            var existingFile = await _context.Files.FirstOrDefaultAsync(f => f.BookId == bookId);
            if (existingFile != null) 
            {
                if (System.IO.File.Exists(existingFile.FilePath))
                {
                    System.IO.File.Delete(existingFile.FilePath);
                }

                _context.Files.Remove(existingFile);
                await _context.SaveChangesAsync();
            }

            //save new file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileMetadata = new FileMetadata
            {
                FileName = fileName,
                FilePath = filePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                BookId = bookId
            };

            var validator = new FileMetaValidator();
            var validationResult = validator.Validate(fileMetadata);

            if (!validationResult.IsValid)
            {
                Console.WriteLine("Validation failed:");
                foreach (var error in validationResult.Errors)
                {
                    Console.WriteLine($"- {error.PropertyName}: {error.ErrorMessage}");
                }
                return BadRequest(validationResult.Errors);
            }               

            _context.Files.Add(fileMetadata);
            await _context.SaveChangesAsync();

            return Ok(new { FileId = fileMetadata.Id, Message = "File uploaded successfully" });
        }

        // file download or read inline 
        [HttpGet("{id}")]
        public async Task<IActionResult> DownloadFile(int id, [FromQuery] bool inline = false)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound("File not found.");

            if (!System.IO.File.Exists(file.FilePath))
                return NotFound("File not found on server.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);

            if (inline)
                return File(fileBytes, file.ContentType);
            return File(fileBytes, file.ContentType, file.FileName);
        }

        // delete file
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound("File not found.");

            if (System.IO.File.Exists(file.FilePath))
                System.IO.File.Delete(file.FilePath);

            _context.Files.Remove(file);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // get file for single book
        [HttpGet("book/{bookId}")]
        public async Task<IActionResult> GetFilesByBook(int bookId)
        {
            var files = await _context.Files
                .Where(f => f.BookId == bookId)
                .ToListAsync();

            return Ok(files);
        }

        //get reading progress for inline reading
        [HttpGet("progress/{fileId}")]
        public async Task<IActionResult> GetReadingProgress(int fileId)
        {
            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userClaim) || !int.TryParse(userClaim, out int userId))
                return Unauthorized("User not identified");

            var progress = await _context.ReadingProgresses
                .FirstOrDefaultAsync(r => r.FileId == fileId && r.UserId == userId);

            if(progress == null)
                return Ok(new {ProgressPercent = 0});

            return Ok(progress);
        }

        //save reading progress for inline reading
        [HttpPost("progress/{fileId}")]
        public async Task<IActionResult> UpdateReadingProgress(int fileId, [FromBody] ReadingProgressUpdateDto dto)
        { 
            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userClaim) || !int.TryParse(userClaim, out int userId))
                return Unauthorized("User not identified.");

            if (dto.ProgressPercent < 0 || dto.ProgressPercent > 100)
                return BadRequest("ProgressPercent must be between 0 and 100.");

            var progress = await _context.ReadingProgresses
                .FirstOrDefaultAsync(r => r.FileId == fileId && r.UserId == userId);

            if (progress == null)
            {
                progress = new Models.ReadingProgress
                {
                    FileId = fileId,
                    UserId = userId,
                    ProgressPercent = dto.ProgressPercent,
                    LastUpdated = DateTime.UtcNow
                };
                _context.ReadingProgresses.Add(progress);
            }
            else
            {
                progress.ProgressPercent = dto.ProgressPercent;
                progress.LastUpdated = DateTime.UtcNow;
                _context.ReadingProgresses.Update(progress);
            }
            await _context.SaveChangesAsync();
            return Ok(progress);
        }
    }
}
