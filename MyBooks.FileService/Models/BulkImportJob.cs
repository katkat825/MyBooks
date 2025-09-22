using MyBooks.Common.BaseClasses;

namespace MyBooks.FileService.Models
{
    public class BulkImportJob : AuditableEntity
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public string? ErrorMessage { get; set; }

        // navigation
        public List<BulkImportItem> Items { get; set; } = new();
    }

    public class BulkImportItem : AuditableEntity
    {
        public int Id { get; set; }
        public int BulkImportJobId { get; set; }
        public BulkImportJob Job { get; set; }

        public string FileId { get; set; }     // Google Drive fileId
        public string FileName { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed
        public string? ErrorMessage { get; set; }

        public int? CreatedBookId { get; set; }
        public int? CreatedFileId { get; set; }
    }
}
