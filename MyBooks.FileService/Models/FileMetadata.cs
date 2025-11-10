using MyBooks.Common.BaseClasses;
using MyBooks.FileService.Services;

namespace MyBooks.FileService.Models;

public class FileMetadata : AuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }

    public int? GoogleIntegrationId { get; set; }
    public GoogleIntegration? GoogleIntegration { get; set; }
    public string? FolderId { get; set; }
    public string? StorageSource { get; set; } = "GoogleDrive";

    public string FileName { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public string FilePath { get; set; }
    public string? ConvertedFilePath { get; set; }
    public bool? IsConverted { get; set; } = false;
    public long FileSize { get; set; }
    public int BookId { get; set; }

    public bool IsActive { get; set; } = true;

    public string UploadedByIp { get; set; }
}

public class StorageSource
{
    public const string GoogleDrive = "GoogleDrive";
    public const string MyBookCatalog = "MyBookCatalog"; // My Book Catalog's Cloudflare R2 instance
}