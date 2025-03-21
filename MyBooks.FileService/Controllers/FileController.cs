using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.Common.Services;
using MyBooks.FileService.Data;
using MyBooks.FileService.Models;
using MyBooks.FileService.Validators;
using System.Text.RegularExpressions;

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

        // 🔹 Upload File
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] int bookId, [FromForm] string bookTitle)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

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
                FileName = sanitizedBookTitle,
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

        // 🔹 Download File
        [HttpGet("{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _context.Files.FindAsync(id);
            if (file == null)
                return NotFound("File not found.");

            if (!System.IO.File.Exists(file.FilePath))
                return NotFound("File not found on server.");

            var fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
            return File(fileBytes, file.ContentType, file.FileName);
        }

        // 🔹 Delete File
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

        // 🔹 List All Files for a Book
        [HttpGet("book/{bookId}")]
        public async Task<IActionResult> GetFilesByBook(int bookId)
        {
            var files = await _context.Files
                .Where(f => f.BookId == bookId)
                .ToListAsync();

            return Ok(files);
        }
    }
}
