using FluentValidation;
using MyBooks.Common.Dtos;
using MyBooks.Common.BaseClasses;

public class UserValidator : AbstractValidator<UserDto>
{
    public UserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.");

        // must be a human-assignable role
        RuleFor(x => x.Role)
            .Must(role => AppRoles.AllRoles.Contains(role))
            .WithMessage("Invalid role specified.");
    }
}
