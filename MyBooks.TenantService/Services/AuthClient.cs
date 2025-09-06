using System.Net.Http;
using System.Net.Http.Json;
using MyBooks.Common.Dtos;

namespace MyBooks.TenantService.Services;

public class AuthClient
{
    private readonly HttpClient _http;

    public AuthClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<int> CreateUserAsync(UserDto request)
    {
        var response = await _http.PostAsJsonAsync("/api/internal/users/create", request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return Convert.ToInt32(data["userId"]);
    }

    public async Task AssignTenantAsync(AssignTenantDto request)
    {
        var response = await _http.PostAsJsonAsync("/api/internal/users/assign-tenant", request);
        response.EnsureSuccessStatusCode();
    }
}
