using MyBooks.Common.BaseClasses;

namespace MyBooks.AuthService.Models
{
    public class User : AuditableEntity
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; } // Used as the username
        public string PasswordHash { get; set; } // Hashed password
        public string Role { get; set; } 
        public int AgeCategoryId { get; set; } // Foreign key from AgeCategories
    }

}
