using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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

        var data = await response.Content.ReadFromJsonAsync<CreatedUserResponseDto>();
        return data?.UserId ?? 0;
    }

    public async Task AssignTenantAsync(AssignTenantDto request)
    {
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
