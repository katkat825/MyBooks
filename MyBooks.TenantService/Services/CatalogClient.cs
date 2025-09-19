namespace MyBooks.TenantService.Services
{
    public class CatalogClient
    {
        private readonly HttpClient _http;

        public CatalogClient(HttpClient http)
        {
            _http = http;
        }

        public async Task SeedDefaultGenresAsync(int tenantId)
        {
            var response = await _http.PostAsync(
                $"api/books/genres/seed/{tenantId}", 
                null);

            response.EnsureSuccessStatusCode();
        }
    }
}
