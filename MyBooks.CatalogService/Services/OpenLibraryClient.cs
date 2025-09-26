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
    private static readonly SemaphoreSlim _semaphore = new(2, 2);

    public OpenLibraryClient(HttpClient httpClient, HtmlSanitizationService sanitizer)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://openlibrary.org/");
        _sanitizer = sanitizer;
    }
    
    public async Task<OpenLibraryBookDto?> LookupByTitleAsync(OpenLibraryLookupDto dto)
    {
        await _semaphore.WaitAsync();
        try
        {
            var url = $"search.json?title={Uri.EscapeDataString(dto.Title)}";
        
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var docs = doc.RootElement.GetProperty("docs");

            Console.WriteLine("Preferred authors: " + string.Join(", ", dto.PreferredAuthors));

            // first try exact title match with author preference
            var match = docs.EnumerateArray()
                .FirstOrDefault(d =>
                    d.TryGetProperty("title", out var t) &&
                    string.Equals(t.GetString(), dto.Title, StringComparison.OrdinalIgnoreCase) &&
                    d.TryGetProperty("author_name", out var authors) &&
                    authors.ValueKind == JsonValueKind.Array &&
                    authors.EnumerateArray().Any(a =>
                        dto.PreferredAuthors.Contains(a.GetString(), StringComparer.OrdinalIgnoreCase)));

            // fallback: exact title (no author filter)
            if (match.ValueKind == JsonValueKind.Undefined)
                match = docs.EnumerateArray()
                    .FirstOrDefault(d =>
                        d.TryGetProperty("title", out var t) &&
                        string.Equals(t.GetString(), dto.Title, StringComparison.OrdinalIgnoreCase));

            // fallback: take first result
            if (match.ValueKind == JsonValueKind.Undefined)
                match = docs.EnumerateArray().FirstOrDefault();

            if (match.ValueKind == JsonValueKind.Undefined)
                return null;

            var isbn = match.TryGetProperty("isbn", out var isbns) && isbns.ValueKind == JsonValueKind.Array
                ? isbns.EnumerateArray()
                    .Select(x => x.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s) && IsbnHelper.IsPlausibleIsbn(s))
                    .OrderByDescending(s => s!.Length) // prefer ISBN-13 over ISBN-10
                    .FirstOrDefault()
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
        finally
        {
            _semaphore.Release();
        }
    }
}