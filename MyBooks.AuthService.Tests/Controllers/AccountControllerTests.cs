using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Controllers;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Dtos;
using MyBooks.AuthService.Models;
using MyBooks.AuthService.Tests.Infrastructure;
using MyBooks.Common.Services;
using Xunit;

namespace MyBooks.AuthService.Tests.Controllers;

public class AccountControllerTests
{
    private static User Seed(AuthDbContext context, int id = 42)
    {
        var user = new User
        {
            Id = id,
            TenantId = 7,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("original"),
            Role = "Owner",
            AgeCategoryId = 3,
            CreatedBy = "seed",
            CreatedDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        context.SaveChangesAsSystemAsync().GetAwaiter().GetResult();
        return user;
    }

    private static AccountController Build(AuthDbContext context, string userId = "42", int tenantId = 7)
        => new(context, new HtmlSanitizationService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = TestPrincipal.For(userId, tenantId)
                }
            }
        };

    [Fact]
    public async Task GetProfile_rejects_a_request_with_no_identity()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        var controller = new AccountController(context, new HtmlSanitizationService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = TestPrincipal.Anonymous() }
            }
        };

        Assert.IsType<UnauthorizedResult>(await controller.GetProfile());
    }

    [Fact]
    public async Task GetProfile_returns_not_found_for_a_missing_user()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.AccessorFor("42", 7));
        var controller = Build(context);

        var result = await controller.GetProfile();

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found.", notFound.Value);
    }

    [Fact]
    public async Task GetProfile_returns_the_user()
    {
        var accessor = TestPrincipal.AccessorFor("42", 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);
        Seed(seed);

        var result = await Build(act).GetProfile();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("ada@example.com", Assert.IsType<User>(ok.Value).Email);
    }

    [Fact]
    public async Task GetProfile_currently_exposes_the_password_hash()
    {
        // Regression guard on a real leak: the endpoint returns the entity as-is. If the
        // response is ever reshaped into a DTO, this test should be inverted.
        var accessor = TestPrincipal.AccessorFor("42", 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);
        Seed(seed);

        var ok = (OkObjectResult)await Build(act).GetProfile();

        Assert.False(string.IsNullOrEmpty(Assert.IsType<User>(ok.Value).PasswordHash));
    }

    [Fact]
    public async Task UpdateProfile_changes_the_name()
    {
        var accessor = TestPrincipal.AccessorFor("42", 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);
        Seed(seed);

        var result = await Build(act).UpdateProfile(new UpdateProfileDto
        {
            FirstName = "Augusta",
            LastName = "King"
        });

        Assert.IsType<OkObjectResult>(result);

        var persisted = await act.Users.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Augusta", persisted.FirstName);
        Assert.Equal("King", persisted.LastName);
    }

    [Fact]
    public async Task UpdateProfile_ignores_null_and_empty_fields()
    {
        var accessor = TestPrincipal.AccessorFor("42", 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);
        Seed(seed);

        await Build(act).UpdateProfile(new UpdateProfileDto
        {
            FirstName = null,
            LastName = string.Empty
        });

        var persisted = await act.Users.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("Ada", persisted.FirstName);
        Assert.Equal("Lovelace", persisted.LastName);
    }

    [Fact]
    public async Task UpdateProfile_strips_html_from_the_name()
    {
        var accessor = TestPrincipal.AccessorFor("42", 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);
        Seed(seed);

        await Build(act).UpdateProfile(new UpdateProfileDto
        {
            FirstName = "<script>alert('xss')</script>Ada"
        });

        var persisted = await act.Users.IgnoreQueryFilters().SingleAsync();
        Assert.DoesNotContain("<", persisted.FirstName);
        Assert.DoesNotContain("script", persisted.FirstName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateProfile_keeps_the_at_sign_in_an_email()
    {
        // The sanitiser strips @ unless explicitly told the field is an address.
        var accessor = TestPrincipal.AccessorFor("42", 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);
        Seed(seed);

        await Build(act).UpdateProfile(new UpdateProfileDto { Email = "new@example.com" });

        var persisted = await act.Users.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("new@example.com", persisted.Email);
    }

    [Fact]
    public async Task UpdateProfile_rehashes_a_new_password()
    {
        var accessor = TestPrincipal.AccessorFor("42", 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);
        Seed(seed);

        await Build(act).UpdateProfile(new UpdateProfileDto { Password = "a brand new password" });

        var persisted = await act.Users.IgnoreQueryFilters().SingleAsync();
        Assert.True(BCrypt.Net.BCrypt.Verify("a brand new password", persisted.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("original", persisted.PasswordHash));
    }

    [Fact]
    public async Task UpdateProfile_records_when_the_policy_was_accepted()
    {
        var accessor = TestPrincipal.AccessorFor("42", 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);
        Seed(seed);

        await Build(act).UpdateProfile(new UpdateProfileDto { AcceptedAup = true });

        var persisted = await act.Users.IgnoreQueryFilters().SingleAsync();
        Assert.True(persisted.AcceptedAup);
        Assert.InRange(persisted.LastAcceptedAup,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task UpdateProfile_cannot_withdraw_policy_acceptance()
    {
        // Documents current behaviour: the flag is one-way. Sending false is a no-op.
        var accessor = TestPrincipal.AccessorFor("42", 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);
        var user = Seed(seed);
        user.AcceptedAup = true;
        await seed.SaveChangesAsSystemAsync();

        await Build(act).UpdateProfile(new UpdateProfileDto { AcceptedAup = false });

        var persisted = await act.Users.IgnoreQueryFilters().SingleAsync();
        Assert.True(persisted.AcceptedAup);
    }
}
