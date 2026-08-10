using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyBooks.FileService.Data;
using NSubstitute;

namespace MyBooks.FileService.Tests.Infrastructure;

public static class TestContext
{
    public const string TenantClaimType = "TenantId";
    public const string RoleClaimType = "role";

    public static ClaimsPrincipal Principal(string userId, int tenantId, string role = "Owner")
        => new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(TenantClaimType, tenantId.ToString()),
            new Claim(RoleClaimType, role)
        }, "test", ClaimTypes.Name, RoleClaimType));

    public static IHttpContextAccessor Accessor(
        string userId, int tenantId, string role = "Owner", string ip = "203.0.113.10")
    {
        var http = new DefaultHttpContext { User = Principal(userId, tenantId, role) };
        http.Request.Headers["X-Forwarded-For"] = ip;

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);
        return accessor;
    }

    /// <summary>
    /// No HttpContext means the audit and tenant hooks short-circuit, which is how
    /// fixture data gets seeded without tripping the security rules under test.
    /// </summary>
    public static IHttpContextAccessor NoContext()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        return accessor;
    }

    public static FileDbContext Db(IHttpContextAccessor accessor, string? name = null)
        => new(new DbContextOptionsBuilder<FileDbContext>()
            .UseInMemoryDatabase(name ?? $"file-{Guid.NewGuid():N}")
            .Options, accessor);

    public static (FileDbContext Seed, FileDbContext Act) DbPair(IHttpContextAccessor actAccessor)
    {
        var name = $"file-{Guid.NewGuid():N}";
        return (Db(NoContext(), name), Db(actAccessor, name));
    }
}
