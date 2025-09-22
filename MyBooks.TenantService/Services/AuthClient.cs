using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Dtos;
using MyBooks.Common.Helpers;

namespace MyBooks.TenantService.Services;

public class AuthClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly SystemTokenHelper _tokenHelper;

    public AuthClient(HttpClient http, IConfiguration config, SystemTokenHelper tokenHelper)
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

    public async Task<int> CreateUserAsync(UserDto request)
    {
        await AddSystemAuthHeaderAsync();

        var response = await _http.PostAsJsonAsync("/api/internal/users/create", request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<CreatedUserResponseDto>();
        return data?.UserId ?? 0;
    }

    public async Task AssignTenantAsync(AssignTenantDto request)
    {
        await AddSystemAuthHeaderAsync();
        
        var response = await _http.PostAsJsonAsync("/api/internal/users/assign-tenant", request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"AuthService returned {(int)response.StatusCode} {response.StatusCode}: {error}");
        }
        response.EnsureSuccessStatusCode();
    }
}
