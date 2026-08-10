using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using Xunit;

namespace MyBooks.AuthService.Tests.Validators;

public class UserValidatorTests
{
    private readonly UserValidator _validator = new();

    private static UserDto Valid() => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "ada@example.com",
        Password = "correct horse battery staple",
        Role = AppRoles.User,
        AgeCategoryId = 3,
        IsActive = true
    };

    [Fact]
    public void Accepts_a_fully_populated_user()
    {
        var result = _validator.Validate(Valid());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Requires_an_email()
    {
        var dto = Valid();
        dto.Email = string.Empty;

        var messages = _validator.Validate(dto).Errors
            .Where(e => e.PropertyName == "Email")
            .Select(e => e.ErrorMessage)
            .ToList();

        // The rule chain is NotEmpty().EmailAddress() with FluentValidation's default
        // Continue cascade, so an empty value trips both rules rather than short-circuiting.
        Assert.Contains("Email is required.", messages);
        Assert.Contains("Invalid email format.", messages);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@tld")]
    [InlineData("@example.com")]
    public void Rejects_malformed_email(string email)
    {
        var dto = Valid();
        dto.Email = email;

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Email" && e.ErrorMessage == "Invalid email format.");
    }

    [Fact]
    public void Requires_first_and_last_name()
    {
        var dto = Valid();
        dto.FirstName = string.Empty;
        dto.LastName = string.Empty;

        var result = _validator.Validate(dto);

        Assert.Contains(result.Errors, e =>
            e.PropertyName == "FirstName" && e.ErrorMessage == "First name is required.");
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LastName" && e.ErrorMessage == "Last name is required.");
    }

    [Theory]
    [InlineData(AppRoles.SuperAdmin)]
    [InlineData(AppRoles.Owner)]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.Editor)]
    [InlineData(AppRoles.User)]
    [InlineData(AppRoles.Support)]
    [InlineData(AppRoles.GlobalReviewer)]
    public void Accepts_every_role_in_AllRoles(string role)
    {
        var dto = Valid();
        dto.Role = role;

        Assert.DoesNotContain(_validator.Validate(dto).Errors, e => e.PropertyName == "Role");
    }

    [Theory]
    [InlineData(AppRoles.TenantService)]
    [InlineData(AppRoles.CatalogService)]
    [InlineData(AppRoles.EmailService)]
    [InlineData(AppRoles.FileService)]
    [InlineData(AppRoles.AuthService)]
    public void Rejects_internal_service_roles(string serviceRole)
    {
        // AllRoles deliberately excludes the service-to-service roles. A user must never
        // be assignable to one, or they would inherit machine privileges.
        var dto = Valid();
        dto.Role = serviceRole;

        Assert.Contains(_validator.Validate(dto).Errors, e =>
            e.PropertyName == "Role" && e.ErrorMessage == "Invalid role specified.");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("OWNER")]
    [InlineData("User ")]
    public void Role_comparison_is_case_and_whitespace_sensitive(string role)
    {
        var dto = Valid();
        dto.Role = role;

        Assert.Contains(_validator.Validate(dto).Errors, e =>
            e.PropertyName == "Role" && e.ErrorMessage == "Invalid role specified.");
    }

    [Fact]
    public void Rejects_an_empty_role()
    {
        // Role has no NotEmpty rule, so an empty value falls through to the Must and
        // still produces the invalid-role message rather than a required message.
        var dto = Valid();
        dto.Role = string.Empty;

        var roleErrors = _validator.Validate(dto).Errors
            .Where(e => e.PropertyName == "Role")
            .Select(e => e.ErrorMessage)
            .ToList();

        Assert.Equal(new[] { "Invalid role specified." }, roleErrors);
    }

    [Fact]
    public void Does_not_validate_password_or_age_category()
    {
        // Documents a real gap: a blank password passes validation here and is hashed
        // downstream without complaint.
        var dto = Valid();
        dto.Password = string.Empty;
        dto.AgeCategoryId = -1;

        var result = _validator.Validate(dto);

        Assert.True(result.IsValid);
    }
}
