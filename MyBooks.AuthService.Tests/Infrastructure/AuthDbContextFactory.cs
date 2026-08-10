using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyBooks.AuthService.Data;

namespace MyBooks.AuthService.Tests.Infrastructure;

public static class AuthDbContextFactory
{
    /// <summary>
    /// A uniquely named in-memory store per call so tests never share state.
    /// </summary>
    public static AuthDbContext Create(IHttpContextAccessor accessor)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase($"auth-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AuthDbContext(options, accessor);
    }

    /// <summary>
    /// Two contexts over the same store: one unauthenticated for seeding, one carrying
    /// the tenant claims under test. Seeding through the authenticated context would
    /// trip the tenant guards in ApplyTenantId.
    /// </summary>
    public static (AuthDbContext Seed, AuthDbContext Act) CreatePair(
        IHttpContextAccessor actAccessor)
    {
        var name = $"auth-{Guid.NewGuid():N}";

        DbContextOptions<AuthDbContext> Build() =>
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(name)
                .Options;

        return (new AuthDbContext(Build(), TestPrincipal.NoContext()),
                new AuthDbContext(Build(), actAccessor));
    }
}
