using MyBooks.Common.BaseClasses;

namespace MyBooks.FileService.Models
{
    public class FileMetadata : AuditableEntity
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        public int? GoogleIntegrationId { get; set; }
        public GoogleIntegration? GoogleIntegration { get; set; }


        public string FileName { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public int BookId { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public string UploadedByIp { get; set; }
    }
}
