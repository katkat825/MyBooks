using MyBooks.Common.BaseClasses;

namespace MyBooks.AuthService.Models;

public class GlobalReviewerAccess : AuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
