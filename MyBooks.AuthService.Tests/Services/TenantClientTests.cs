using System.Net;
using Microsoft.Extensions.Configuration;
using MyBooks.AuthService.Services;
using MyBooks.AuthService.Tests.Infrastructure;
using MyBooks.Common.Helpers;
using Xunit;

namespace MyBooks.AuthService.Tests.Services;

public class TenantClientTests
{
    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServiceSecrets:AuthService"] = "auth-secret"
        }).Build();

    private static (TenantClient Client, StubHttpMessageHandler Handler) Build(
        HttpStatusCode status, string body)
    {
        var handler = StubHttpMessageHandler.WithSystemToken(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://tenants") };
        var tokenHelper = new SystemTokenHelper(
            new HttpClient(StubHttpMessageHandler.WithSystemToken(HttpStatusCode.OK))
            { BaseAddress = new Uri("http://auth") },
            "http://auth");

        return (new TenantClient(http, Config(), tokenHelper), handler);
    }

    [Fact]
    public async Task GetMaxUserCount_reads_the_seat_limit()
    {
        var (client, _) = Build(HttpStatusCode.OK, "5");

        Assert.Equal(5, await client.GetMaxUserCountAsync(7));
    }

    [Fact]
    public async Task GetMaxUserCount_throws_when_the_tenant_service_fails()
    {
        // Deliberately not swallowed: a failed quota lookup must not be read as
        // "unlimited seats" by the caller.
        var (client, _) = Build(HttpStatusCode.InternalServerError, "");

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMaxUserCountAsync(7));
    }

    [Fact]
    public async Task GetMaxUserCount_calls_the_system_endpoint_for_the_tenant()
    {
        var (client, handler) = Build(HttpStatusCode.OK, "5");

        await client.GetMaxUserCountAsync(7);

        Assert.True(handler.SentTo("/system/7/max-users"));
    }

    [Fact]
    public async Task GetTenantLookup_returns_the_tenant()
    {
        var (client, _) = Build(HttpStatusCode.OK, "{\"id\":7,\"isActive\":true}");

        var tenant = await client.GetTenantLookupAsync(7);

        Assert.NotNull(tenant);
        Assert.Equal(7, tenant!.Id);
        Assert.True(tenant.IsActive);
    }

    [Fact]
    public async Task GetTenantLookup_returns_null_rather_than_throwing()
    {
        // Login depends on this: an unresolvable tenant becomes a 401, not a 500.
        var (client, _) = Build(HttpStatusCode.NotFound, "");

        Assert.Null(await client.GetTenantLookupAsync(7));
    }

    [Fact]
    public async Task GetTenantLookup_is_anonymous_even_after_an_authenticated_call()
    {
        // The client is a long-lived typed HttpClient, so a stale Authorization header
        // would otherwise leak a system token onto the public lookup endpoint.
        var (client, handler) = Build(HttpStatusCode.OK, "{\"id\":7,\"isActive\":true}");

        await client.GetMaxUserCountAsync(7);
        await client.GetTenantLookupAsync(7);

        var anonymousCall = handler.Requests.Last();
        Assert.Null(anonymousCall.Headers.Authorization);
    }
}
