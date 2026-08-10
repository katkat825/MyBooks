using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MyBooks.AuthService.Controllers;
using MyBooks.AuthService.Data;
using MyBooks.AuthService.Dtos;
using MyBooks.AuthService.Models;
using MyBooks.AuthService.Services;
using MyBooks.AuthService.Tests.Infrastructure;
using MyBooks.Common.Helpers;
using Xunit;

namespace MyBooks.AuthService.Tests.Controllers;

public class LoginControllerTests
{
    private const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-256";
    private const string Password = "correct horse battery staple";

    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = SigningKey,
            ["Jwt:Issuer"] = "MyBooks",
            ["Jwt:Audience"] = "MyBooksUsers",
            ["ServiceSecrets:AuthService"] = "auth-secret",
            ["ServiceUrls:TenantService"] = "http://tenants"
        }).Build();

    /// <summary>
    /// TenantClient has no interface and no virtual members, so it is built for real over
    /// a scripted transport rather than substituted.
    /// </summary>
    private static TenantClient TenantClientReturning(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = StubHttpMessageHandler.WithSystemToken(status, json);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://tenants") };
        var tokenHelper = new SystemTokenHelper(
            new HttpClient(StubHttpMessageHandler.WithSystemToken(HttpStatusCode.OK))
            { BaseAddress = new Uri("http://auth") },
            "http://auth");

        return new TenantClient(http, Config(), tokenHelper);
    }

    private static User SeedUser(AuthDbContext context, Action<User>? tweak = null)
    {
        var user = new User
        {
            TenantId = 7,
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            Role = "Owner",
            AgeCategoryId = 3,
            IsActive = true,
            IsVisible = true,
            CreatedBy = "seed",
            CreatedDate = DateTime.UtcNow
        };

        tweak?.Invoke(user);
        context.Users.Add(user);
        context.SaveChangesAsSystemAsync().GetAwaiter().GetResult();
        return user;
    }

    private static LoginController Build(AuthDbContext context, TenantClient tenantClient)
        => new(context, Config(), tenantClient);

    [Fact]
    public async Task Rejects_an_unknown_email()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        var controller = Build(context, TenantClientReturning("{\"id\":7,\"isActive\":true}"));

        var result = await controller.Login(new LoginDto
        {
            Email = "nobody@example.com",
            Password = Password
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid username or password.", unauthorized.Value);
    }

    [Fact]
    public async Task Rejects_a_wrong_password()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        SeedUser(context);
        var controller = Build(context, TenantClientReturning("{\"id\":7,\"isActive\":true}"));

        var result = await controller.Login(new LoginDto
        {
            Email = "ada@example.com",
            Password = "wrong"
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        // Deliberately the same message as an unknown email, so the response cannot be
        // used to enumerate which addresses have accounts.
        Assert.Equal("Invalid username or password.", unauthorized.Value);
    }

    [Fact]
    public async Task Email_lookup_is_case_and_whitespace_insensitive()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        SeedUser(context);
        var controller = Build(context, TenantClientReturning("{\"id\":7,\"isActive\":true}"));

        var result = await controller.Login(new LoginDto
        {
            Email = "  ADA@Example.com  ",
            Password = Password
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Rejects_a_deactivated_user()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        SeedUser(context, u => u.IsActive = false);
        var controller = Build(context, TenantClientReturning("{\"id\":7,\"isActive\":true}"));

        var result = await controller.Login(new LoginDto
        {
            Email = "ada@example.com",
            Password = Password
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Your account has been deactivated.", unauthorized.Value);
    }

    [Fact]
    public async Task Rejects_a_user_with_no_tenant()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        SeedUser(context, u => u.TenantId = null);
        var controller = Build(context, TenantClientReturning("{\"id\":7,\"isActive\":true}"));

        var result = await controller.Login(new LoginDto
        {
            Email = "ada@example.com",
            Password = Password
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User is not assigned to an account.", unauthorized.Value);
    }

    [Fact]
    public async Task Rejects_a_login_against_a_deactivated_tenant()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        SeedUser(context);
        var controller = Build(context, TenantClientReturning("{\"id\":7,\"isActive\":false}"));

        var result = await controller.Login(new LoginDto
        {
            Email = "ada@example.com",
            Password = Password
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Account is deactivated.", unauthorized.Value);
    }

    [Fact]
    public async Task Rejects_a_login_when_the_tenant_cannot_be_resolved()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        SeedUser(context);
        var controller = Build(context, TenantClientReturning("", HttpStatusCode.NotFound));

        var result = await controller.Login(new LoginDto
        {
            Email = "ada@example.com",
            Password = Password
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Account is deactivated.", unauthorized.Value);
    }

    [Fact]
    public async Task A_soft_deleted_user_can_still_authenticate()
    {
        // Documents current behaviour rather than endorsing it: the lookup calls
        // IgnoreQueryFilters, so IsVisible = false does not block login on its own.
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        SeedUser(context, u => u.IsVisible = false);
        var controller = Build(context, TenantClientReturning("{\"id\":7,\"isActive\":true}"));

        var result = await controller.Login(new LoginDto
        {
            Email = "ada@example.com",
            Password = Password
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Successful_login_returns_a_token()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        SeedUser(context);
        var controller = Build(context, TenantClientReturning("{\"id\":7,\"isActive\":true}"));

        var result = await controller.Login(new LoginDto
        {
            Email = "ada@example.com",
            Password = Password
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var token = ok.Value!.GetType().GetProperty("Token")!.GetValue(ok.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Generated_token_carries_the_claims_the_guards_depend_on()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        var controller = Build(context, TenantClientReturning("{}"));

        var raw = controller.GenerateJwtToken(new User
        {
            Id = 42,
            TenantId = 7,
            Email = "ada@example.com",
            Role = "Owner",
            AgeCategoryId = 3,
            IsActive = true,
            AcceptedAup = true,
            FirstName = "Ada",
            LastName = "Lovelace",
            PasswordHash = "irrelevant"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(raw);

        Assert.Equal("42", jwt.Claims.Single(c => c.Type == "nameid").Value);
        Assert.Equal("ada@example.com", jwt.Claims.Single(c => c.Type == "email").Value);
        Assert.Equal("Owner", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("7", jwt.Claims.Single(c => c.Type == "TenantId").Value);
        Assert.Equal("3", jwt.Claims.Single(c => c.Type == "AgeCategoryId").Value);
        Assert.Equal("True", jwt.Claims.Single(c => c.Type == "IsActive").Value);
        Assert.Equal("True", jwt.Claims.Single(c => c.Type == "AcceptedAup").Value);
    }

    [Fact]
    public void Generated_token_expires_in_twelve_hours()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        var controller = Build(context, TenantClientReturning("{}"));

        var raw = controller.GenerateJwtToken(new User
        {
            Id = 1, TenantId = 7, Email = "a@b.c", Role = "User",
            FirstName = "A", LastName = "B", PasswordHash = "x"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(raw);
        Assert.InRange(jwt.ValidTo, DateTime.UtcNow.AddHours(11), DateTime.UtcNow.AddHours(13));
    }

    [Fact]
    public void Token_for_a_user_with_no_tenant_emits_an_empty_tenant_claim()
    {
        // int?.ToString() yields "" rather than null, so the claim is present but blank.
        // Any consumer parsing it must treat blank as "no tenant" and not as zero.
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());
        var controller = Build(context, TenantClientReturning("{}"));

        var raw = controller.GenerateJwtToken(new User
        {
            Id = 1, TenantId = null, Email = "a@b.c", Role = "User",
            FirstName = "A", LastName = "B", PasswordHash = "x"
        });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(raw);
        var tenantClaim = jwt.Claims.SingleOrDefault(c => c.Type == "TenantId");

        Assert.NotNull(tenantClaim);
        Assert.Equal(string.Empty, tenantClaim!.Value);
    }
}
