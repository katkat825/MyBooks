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
            RuleFor(file => file)
                .NotNull().WithMessage("No file uploaded.");

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

            // file signature validation
            RuleFor(file => file)
                .Must(HasValidSignature)
                .WithMessage("File signature does not match the expected format.");
        }

        private bool HasValidSignature(IFormFile file)
        {
            //get expected signature based on extension
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                return false;

            byte[] header = new byte[8];
            using (var stream = file.OpenReadStream())
            {
                if (stream.Read(header, 0, header.Length) < header.Length)
                    return false;
            }

            switch (extension) 
            {
                case ".pdf":
                    // PDF files start with "%PDF-" (hex: 25 50 44 46 2D)
                    var pdfSignature = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
                    return header.Take(5).SequenceEqual(pdfSignature);

                case ".epub":
                    // EPUB is a zip archive, so it should start with "PK" (hex: 50 4B)
                    var zipSignature = new byte[] { 0x50, 0x4B };
                    return header.Take(2).SequenceEqual(zipSignature);

                case ".mobi":
                    // MOBI files are more complex. A common approach is to check for the "BOOKMOBI" string in header
                    string headerText = System.Text.Encoding.ASCII.GetString(header);
                    return headerText.Contains("BOOKMOBI");

                case ".txt":
                    // For plain text files, there isn't a standard signature.
                    return true;

                default:
                    return false;
            }
        }
    }
}

