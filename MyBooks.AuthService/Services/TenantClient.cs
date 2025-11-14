using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Helpers;

namespace MyBooks.AuthService.Services;

public class TenantClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly SystemTokenHelper _tokenHelper;

    public TenantClient(HttpClient http, IConfiguration config, SystemTokenHelper tokenHelper)
    {
        _http = http;
        _config = config;
        _tokenHelper = tokenHelper;
    }

    private async Task AddSystemAuthHeaderAsync()
    {
        var token = await _tokenHelper.GetSystemTokenAsync(
            AppRoles.AuthService,
            _config["ServiceSecrets:AuthService"]);

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<int> GetMaxUserCountAsync(int tenantId)
    {
        await AddSystemAuthHeaderAsync();

        var response = await _http.GetAsync($"/system/{tenantId}/max-users");
        response.EnsureSuccessStatusCode();

        var count = await response.Content.ReadFromJsonAsync<int>();
        return count;
    }
}
