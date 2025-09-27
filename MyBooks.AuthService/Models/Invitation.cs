using MyBooks.Common.BaseClasses;

namespace MyBooks.AuthService.Models;

public class Invitation : AuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }

    public string Email { get; set; } = null!;
    public string InvitationToken { get; set; } = null!;

    public DateTime ExpirationDate { get; set; }

    public bool IsActive { get; set; }
    public string? DeactivationReason { get; set; }
}

public class InvitationDeactivationReason
{
    public const string Used = "Used";
    public const string Expired = "Expired";
    public const string Replaced = "Replaced";
    public const string EmailFailed = "EmailFailed";
}