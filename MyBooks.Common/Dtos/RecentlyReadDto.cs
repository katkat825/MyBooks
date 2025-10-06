namespace MyBooks.Common.Dtos;

public class RecentlyReadDto
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Genre { get; set; }
    public string? Series { get; set; }
    public double ProgressPercent { get; set; }
    public int? FileId { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ReadingProgressDto
{
    public int FileId { get; set; }
    public double ProgressPercent { get; set; }
    public DateTime LastUpdated { get; set; }
}
