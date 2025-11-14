using System.Net.Http.Json;

namespace MyBooks.Common.Helpers;

public class SystemTokenHelper
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public SystemTokenHelper(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl ?? throw new InvalidOperationException("AuthService base URL missing");
    }

    public async Task<string> GetSystemTokenAsync(string serviceName, string serviceSecret)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/system/token");
        request.Headers.Add("X-Service-Name", serviceName);
        request.Headers.Add("X-Service-Secret", serviceSecret);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<SystemTokenResponse>();
        return json?.Token ?? throw new InvalidOperationException("System token not returned");
    }
}

public record SystemTokenResponse(string Token);
