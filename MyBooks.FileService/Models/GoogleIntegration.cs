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
        public string? AccessToken { get; set; } // optional: cached short-lived token
        public DateTime? AccessTokenExpiry { get; set; }

        // optional scoping
        public string? DriveFolderId { get; set; } // root folder where books are stored

        public bool IsActive { get; set; } = true;
    }
}
