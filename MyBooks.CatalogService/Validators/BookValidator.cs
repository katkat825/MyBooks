using FluentValidation;
using MyBooks.CatalogService.Models;

namespace MyBooks.CatalogService.Validators
{
    public class BookValidator : AbstractValidator<Book>
    {
        public BookValidator() 
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .Length(3, 100).WithMessage("Title must be between 3 and 100 characters.");

            RuleFor(x => x.Author)
                .Length(3, 100).WithMessage("Author name must be between 3 and 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.Author));

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
                .When(x => !string.IsNullOrEmpty(x.Description));

            RuleFor(x => x.ISBN)
                .Matches(@"^\d{13}$").WithMessage("ISBN must be a valid 13-digit number.")
                .When(x => !string.IsNullOrEmpty(x.ISBN));

            RuleFor(x => x.Location)
                .MaximumLength(500).WithMessage("Location cannot exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.Location));

            RuleFor(x => x.GenreId)
                .GreaterThan(0).WithMessage("Genre is required.");

            RuleFor(x => x.AgeCategoryId)
                .GreaterThan(0).WithMessage("Age Category is required.");

            RuleFor(x => x.TagInput)
                .Must(x => x == null || x.Split(',').Length <= 10)
                .WithMessage("You can add up to 10 tags.")
                .Must(x => x == null || x.Split(',')
                    .All(t => t.Trim().Length <= 25))
                .WithMessage("Each tag cannot exceed 25 characters.");
        }
    }
}
