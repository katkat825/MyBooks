namespace MyBooks.Common.Dtos;

public class BookImportRequestDto
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public int GenreId { get; set; }
    public int AgeCategoryId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TenantId { get; set; }
}

public class BookImportResponseDto
{
    public int BookId { get; set; }
    public string FilePath { get; set; }
}

public class BookFileLinkDto
{
    public int BookId { get; set; }
    public int FileId { get; set; }
}

public class BulkImportStartDto
{
    public List<string> FileIds { get; set; } = new();
    public int GenreId { get; set; }
    public int AgeCategoryId { get; set; }
    public int IntegrationId { get; set; }
    
    // Optional: allow per-file overrides if UI adds them later
    public List<BulkImportFileOverrideDto>? Overrides { get; set; }
}

public class BulkImportFileOverrideDto
{
    public string FileId { get; set; }
    public int? GenreId { get; set; }
    public int? AgeCategoryId { get; set; }
}

public class FileScanDto
{
    public string UserId { get; set; }
    public int TenantId { get; set; }
    public string IpAddress { get; set; }
    public int IntegrationId { get; set; }
    public BulkImportStartDto BulkImportStart { get; set; }
}