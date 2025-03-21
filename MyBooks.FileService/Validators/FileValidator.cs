using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace MyBooks.FileService.Validators
{
    public class FileValidator : AbstractValidator<IFormFile>
    {
        public FileValidator()
        {
            // Validate that a file was uploaded
            RuleFor(file => file)
                .NotNull().WithMessage("No file uploaded.");

            // Validate file size (example: must be greater than 0 and below 1GB)
            RuleFor(file => file.Length)
                .GreaterThan(0).WithMessage("File must not be empty.")
                .LessThanOrEqualTo(1073741824).WithMessage("File size must be below 1GB.");

            // Validate file name: non-empty, reasonable length, and allowed extension
            RuleFor(file => file.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .MaximumLength(255).WithMessage("File name must not exceed 255 characters.")
                .Must(fileName =>
                {
                    var allowedExtensions = new[] { ".pdf", ".epub", ".mobi", ".txt" };
                    var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
                    return !string.IsNullOrEmpty(extension) && allowedExtensions.Contains(extension);
                })
                .WithMessage("Invalid file extension. Allowed extensions are: .pdf, .epub, .mobi, .txt.");

            // Validate MIME type: non-empty and one of the allowed types
            RuleFor(file => file.ContentType)
                .NotEmpty().WithMessage("Content type is required.")
                .Must(contentType =>
                {
                    var allowedMimeTypes = new[]
                    {
                        "application/pdf",                // PDF
                        "application/epub+zip",           // EPUB
                        "application/x-mobipocket-ebook", // MOBI
                        "text/plain"                      // TXT
                    };
                    return allowedMimeTypes.Contains(contentType);
                })
                .WithMessage("Invalid MIME type. Allowed types are: application/pdf, application/epub+zip, application/x-mobipocket-ebook, text/plain.");
        }
    }
}

