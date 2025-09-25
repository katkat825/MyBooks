using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MyBooks.CatalogService.Models;
using MyBooks.Common.Services;

namespace MyBooks.CatalogService.Services;

public class OpenLibraryClient
{
    private readonly HttpClient _httpClient;
    private readonly HtmlSanitizationService _sanitizer;

    public OpenLibraryClient(HttpClient httpClient, HtmlSanitizationService sanitizer)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://openlibrary.org/");
        _sanitizer = sanitizer;
    }
    
    public async Task<OpenLibraryBookDto?> LookupByTitleAsync(string title)
    {
        var url = $"search.json?title={Uri.EscapeDataString(title)}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var docs = doc.RootElement.GetProperty("docs");

            // first try exact title match (case-insensitive)
            var match = docs.EnumerateArray()
                .FirstOrDefault(d =>
                    d.TryGetProperty("title", out var t) &&
                    string.Equals(t.GetString(), title, StringComparison.OrdinalIgnoreCase));

            // fallback: take first result
            if (match.ValueKind == JsonValueKind.Undefined)
                match = docs.EnumerateArray().FirstOrDefault();

            if (match.ValueKind == JsonValueKind.Undefined)
                return null;

            var isbn = match.TryGetProperty("isbn", out var isbns) && isbns.ValueKind == JsonValueKind.Array
                ? isbns.EnumerateArray().Select(x => x.GetString()).FirstOrDefault(IsbnHelper.IsPlausibleIsbn)
                : null;

            var result = new OpenLibraryBookDto
            {
                Title = match.TryGetProperty("title", out var t) ? _sanitizer.Sanitize(t.GetString()) : null,
                Author = match.TryGetProperty("author_name", out var authors) && authors.ValueKind == JsonValueKind.Array
                    ? _sanitizer.Sanitize(authors.EnumerateArray().FirstOrDefault().GetString())
                    : null,
                ISBN = isbn,
                PublishedDate = match.TryGetProperty("first_publish_year", out var year) && year.TryGetInt32(out var y)
                    ? new DateTime(y, 1, 1)
                    : null
            };

            return result;
        }
        catch
        {
            return null;
        }
    }
}