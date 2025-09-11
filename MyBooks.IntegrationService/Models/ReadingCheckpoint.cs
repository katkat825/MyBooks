namespace MyBooks.IntegrationService.Models;

public class ReadingCheckpoint
{
    public int Id { get; set; }
    public int TenantId { get; set; } 
    public int UserId { get; set; }
    public int BookId { get; set; }

    public int? LastPage { get; set; }
    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;
}
