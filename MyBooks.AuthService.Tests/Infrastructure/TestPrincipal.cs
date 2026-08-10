using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace MyBooks.AuthService.Tests.Infrastructure;

/// <summary>
/// Builds the claims principals the services expect. Tokens issued by this app use a
/// claim literally named "role", so IsInRole only works when the identity is
/// constructed with that role type.
/// </summary>
public static class TestPrincipal
{
    public const string RoleClaimType = "role";
    public const string TenantClaimType = "TenantId";

    public static ClaimsPrincipal For(string userId, int tenantId, string role = "Owner")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(TenantClaimType, tenantId.ToString()),
            new(RoleClaimType, role)
        };

        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, "test", ClaimTypes.Name, RoleClaimType));
    }

    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    public static IHttpContextAccessor AccessorFor(string userId, int tenantId, string role = "Owner")
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { User = For(userId, tenantId, role) });
        return accessor;
    }

    /// <summary>
    /// An accessor with no HttpContext. Both DbContexts skip tenant stamping and audit
    /// entirely in that case, which is the cheapest way to seed fixture data.
    /// </summary>
    public static IHttpContextAccessor NoContext()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        return accessor;
    }
}
