using FluentValidation;
using MyBooks.FileService.Models;

namespace MyBooks.FileService.Validators
{
    public class FileMetaValidator : AbstractValidator<FileMetadata>
    {
        public FileMetaValidator() 
        {
            RuleFor(f => f.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .MaximumLength(255).WithMessage("File name must not exceed 255 characters.");

            RuleFor(f => f.FileSize)
               .GreaterThan(0).WithMessage("File must not be empty.")
               .LessThanOrEqualTo(1073741824).WithMessage("File size must be below 1GB.");

            RuleFor(f => f.BookId)
                .GreaterThan(0).WithMessage("Book ID is required.");
        }
    }
}
