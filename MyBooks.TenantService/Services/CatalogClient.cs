using MyBooks.Common.Helpers;
using MyBooks.Common.BaseClasses;
using System.Net.Http.Headers;

namespace MyBooks.TenantService.Services;

public class CatalogClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly SystemTokenHelper _tokenHelper;

    public CatalogClient(HttpClient http, IConfiguration config, SystemTokenHelper tokenHelper)
    {
        _http = http;
        _config = config;
        _tokenHelper = tokenHelper;
    }

    private async Task AddSystemAuthHeaderAsync()
    {
        var token = await _tokenHelper.GetSystemTokenAsync(
            AppRoles.TenantService,
            _config["ServiceSecrets:TenantService"]);

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task SeedDefaultGenresAsync(int tenantId)
    {
        await AddSystemAuthHeaderAsync();
        
        var response = await _http.PostAsync(
            $"/genres/{tenantId}/seed",
            null);

        response.EnsureSuccessStatusCode();
    }
}