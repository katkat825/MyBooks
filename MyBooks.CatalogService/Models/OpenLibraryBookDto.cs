namespace MyBooks.CatalogService.Models;

public class OpenLibraryBookDto
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? ISBN { get; set; }
    public string? SeriesName { get; set; }
    public string? SeriesIndex { get; set; }
}

public class SeriesDto
{
    public int? SeriesId { get; set; }
    public decimal? SeriesPosition { get; set; }
}

public class OpenLibraryLookupDto
{
    public string Title { get; set; } = null!;
    public List<string> PreferredAuthors { get; set; } = new();
}