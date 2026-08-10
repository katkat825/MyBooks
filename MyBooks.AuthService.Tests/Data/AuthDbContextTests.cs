using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Models;
using MyBooks.AuthService.Tests.Infrastructure;
using NSubstitute;
using Xunit;

namespace MyBooks.AuthService.Tests.Data;

/// <summary>
/// The tenant query filter and the audit/soft-delete behaviour in SaveChanges are the
/// load-bearing security boundary for this service. Everything else assumes they hold.
/// </summary>
public class AuthDbContextTests
{
    private static User NewUser(int tenantId, string email = "user@example.com") => new()
    {
        TenantId = tenantId,
        FirstName = "Test",
        LastName = "User",
        Email = email,
        PasswordHash = "irrelevant",
        Role = "User",
        AgeCategoryId = 3,
        CreatedBy = "seed",
        CreatedDate = DateTime.UtcNow
    };

    [Fact]
    public async Task Users_from_another_tenant_are_invisible()
    {
        var accessor = TestPrincipal.AccessorFor("1", tenantId: 10);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);

        seed.Users.AddRange(NewUser(10, "mine@example.com"), NewUser(99, "theirs@example.com"));
        await seed.SaveChangesAsync();

        var visible = await act.Users.ToListAsync();

        Assert.Single(visible);
        Assert.Equal("mine@example.com", visible[0].Email);
    }

    [Fact]
    public async Task Invisible_users_are_filtered_out()
    {
        var accessor = TestPrincipal.AccessorFor("1", tenantId: 10);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);

        var deleted = NewUser(10, "deleted@example.com");
        deleted.IsVisible = false;
        seed.Users.AddRange(NewUser(10, "live@example.com"), deleted);
        await seed.SaveChangesAsync();

        var visible = await act.Users.ToListAsync();

        Assert.Single(visible);
        Assert.Equal("live@example.com", visible[0].Email);
    }

    [Fact]
    public async Task Users_with_no_tenant_are_never_visible()
    {
        // TenantId is int? but the filter compares against an int, so nulls can never match.
        var accessor = TestPrincipal.AccessorFor("1", tenantId: 10);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);

        var orphan = NewUser(10, "orphan@example.com");
        orphan.TenantId = null;
        seed.Users.Add(orphan);
        await seed.SaveChangesAsync();

        Assert.Empty(await act.Users.ToListAsync());
    }

    [Fact]
    public async Task IgnoreQueryFilters_reaches_across_tenants()
    {
        var accessor = TestPrincipal.AccessorFor("1", tenantId: 10);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);

        seed.Users.AddRange(NewUser(10), NewUser(99, "other@example.com"));
        await seed.SaveChangesAsync();

        Assert.Equal(2, await act.Users.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public void GetCurrentTenantId_returns_zero_without_a_context()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());

        Assert.Equal(0, context.GetCurrentTenantId());
        Assert.Null(context.GetCurrentUserId());
    }

    [Fact]
    public void GetCurrentTenantId_returns_zero_for_an_unparsable_claim()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim("TenantId", "not-a-number")
        }, "test");
        accessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });

        using var context = AuthDbContextFactory.Create(accessor);

        Assert.Equal(0, context.GetCurrentTenantId());
    }

    [Fact]
    public async Task SaveChanges_stamps_the_tenant_from_the_caller_claims()
    {
        var accessor = TestPrincipal.AccessorFor("42", tenantId: 7);
        using var context = AuthDbContextFactory.Create(accessor);

        var user = NewUser(0);
        user.TenantId = null;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        Assert.Equal(7, user.TenantId);
    }

    [Fact]
    public async Task SaveChanges_stamps_audit_fields_from_the_caller()
    {
        var accessor = TestPrincipal.AccessorFor("42", tenantId: 7);
        using var context = AuthDbContextFactory.Create(accessor);

        var user = NewUser(7);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        Assert.Equal("42", user.CreatedBy);
        Assert.InRange(user.CreatedDate, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task SaveChanges_refuses_an_authenticated_request_with_no_tenant()
    {
        var accessor = TestPrincipal.AccessorFor("42", tenantId: 0);
        using var context = AuthDbContextFactory.Create(accessor);

        context.Users.Add(NewUser(0));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
        Assert.Equal("SaveChanges requires valid tenant.", ex.Message);
    }

    [Fact]
    public async Task SaveChanges_refuses_a_request_with_no_user_identity()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("TenantId", "7") }, "test"))
        });

        using var context = AuthDbContextFactory.Create(accessor);
        context.Users.Add(NewUser(7));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
        Assert.Equal("SaveChanges requires valid authenticated user.", ex.Message);
    }

    [Fact]
    public async Task Deleting_a_user_soft_deletes_and_mangles_the_email()
    {
        var accessor = TestPrincipal.AccessorFor("42", tenantId: 7);
        var (seed, act) = AuthDbContextFactory.CreatePair(accessor);

        var user = NewUser(7, "gone@example.com");
        seed.Users.Add(user);
        await seed.SaveChangesAsync();

        var tracked = await act.Users.SingleAsync();
        act.Users.Remove(tracked);
        await act.SaveChangesAsync();

        // The row survives; the unique email index is freed by suffixing the address so
        // the same person can be re-invited later.
        var persisted = await act.Users.IgnoreQueryFilters().SingleAsync();
        Assert.False(persisted.IsVisible);
        Assert.StartsWith("gone@example.com_old_", persisted.Email);
        Assert.EndsWith(DateTime.UtcNow.ToString("yyyyMMdd"), persisted.Email);
    }

    [Fact]
    public async Task System_save_requires_CreatedDate_to_be_set()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());

        var user = NewUser(7);
        user.CreatedDate = default;
        context.Users.Add(user);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsSystemAsync());
        Assert.Equal("System save requires CreatedBy and CreatedDate to be set.", ex.Message);
    }

    [Fact]
    public async Task System_save_succeeds_when_audit_fields_are_pre_populated()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());

        context.Users.Add(NewUser(7));

        var written = await context.SaveChangesAsSystemAsync();

        Assert.Equal(1, written);
    }

    [Fact]
    public async Task Superadmin_save_rejects_an_entity_with_no_tenant()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());

        var user = NewUser(0);
        user.TenantId = null;
        context.Users.Add(user);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsSuperadminAsync());
        Assert.Equal("Superadmin save requires a valid TenantId.", ex.Message);
    }

    [Fact]
    public async Task Superadmin_save_accepts_an_explicit_tenant()
    {
        using var context = AuthDbContextFactory.Create(TestPrincipal.NoContext());

        context.Users.Add(NewUser(7));

        Assert.Equal(1, await context.SaveChangesAsSuperadminAsync());
    }
}
