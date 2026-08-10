using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MyBooks.AuthService.Controllers;
using Xunit;

namespace MyBooks.AuthService.Tests.Controllers;

/// <summary>
/// This endpoint is the root of trust for every service-to-service call in the system.
/// If it hands a token to the wrong caller, tenant isolation everywhere else is moot.
/// </summary>
public class SystemTokenControllerTests
{
    private const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-256";

    private static SystemTokenController Build(params (string Key, string Value)[] extra)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = SigningKey,
            ["Jwt:Issuer"] = "MyBooks",
            ["Jwt:Audience"] = "MyBooksUsers",
            ["ServiceSecrets:FileService"] = "file-secret",
            ["ServiceSecrets:CatalogService"] = "catalog-secret"
        };

        foreach (var (key, value) in extra)
            settings[key] = value;

        return new SystemTokenController(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    [Theory]
    [InlineData(null, "file-secret")]
    [InlineData("", "file-secret")]
    [InlineData("   ", "file-secret")]
    [InlineData("FileService", null)]
    [InlineData("FileService", "")]
    public void Rejects_missing_credentials(string? name, string? secret)
    {
        var result = Build().GetSystemToken(name!, secret!);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Missing service name or secret.", bad.Value);
    }

    [Fact]
    public void Rejects_an_unknown_service_name()
    {
        // The config key is interpolated from the caller-supplied header, so an unknown
        // name resolves to a null secret rather than matching anything.
        var result = Build().GetSystemToken("NoSuchService", "file-secret");

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Rejects_a_wrong_secret()
    {
        Assert.IsType<UnauthorizedResult>(Build().GetSystemToken("FileService", "not-the-secret"));
    }

    [Fact]
    public void Secret_comparison_is_case_sensitive()
    {
        Assert.IsType<UnauthorizedResult>(Build().GetSystemToken("FileService", "FILE-SECRET"));
    }

    [Fact]
    public void Does_not_leak_a_catalog_token_to_the_file_service_secret()
    {
        // Cross-wiring the pairs must fail in both directions.
        Assert.IsType<UnauthorizedResult>(Build().GetSystemToken("FileService", "catalog-secret"));
        Assert.IsType<UnauthorizedResult>(Build().GetSystemToken("CatalogService", "file-secret"));
    }

    [Fact]
    public void Issues_a_token_for_a_matching_secret()
    {
        var result = Build().GetSystemToken("FileService", "file-secret");

        var ok = Assert.IsType<OkObjectResult>(result);
        var token = ok.Value!.GetType().GetProperty("token")!.GetValue(ok.Value) as string;
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Issued_token_carries_the_service_name_as_subject_and_role()
    {
        var ok = (OkObjectResult)Build().GetSystemToken("FileService", "file-secret");
        var raw = (string)ok.Value!.GetType().GetProperty("token")!.GetValue(ok.Value)!;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(raw);

        Assert.Equal("FileService", jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal("FileService", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("MyBooks", jwt.Issuer);
    }

    [Fact]
    public void Issued_token_is_short_lived()
    {
        var ok = (OkObjectResult)Build().GetSystemToken("FileService", "file-secret");
        var raw = (string)ok.Value!.GetType().GetProperty("token")!.GetValue(ok.Value)!;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(raw);

        // Five minutes. A machine token that outlives the request it was minted for is a
        // standing credential in the logs of whatever it called.
        Assert.InRange(jwt.ValidTo, DateTime.UtcNow.AddMinutes(4), DateTime.UtcNow.AddMinutes(6));
    }
}
