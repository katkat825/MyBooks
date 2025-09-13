using MyBooks.Common.BaseClasses;

namespace MyBooks.FileService.Models
{
    public class GoogleIntegration : AuditableEntity
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        // which Google account this integration belongs to
        public string AccountEmail { get; set; }  

        // auth
        public string RefreshToken { get; set; }
        public string? AccessToken { get; set; } 
        public DateTime? AccessTokenExpiry { get; set; }

        public List<string>? DriveFolderIds { get; set; } // folders connected where books live

        public bool IsActive { get; set; } = true;
    }
}
