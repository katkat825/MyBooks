using MyBooks.Common.BaseClasses;

namespace MyBooks.SupportService.Models;

public class ImpersonationLog : AuditableEntity
{
    public int Id { get; set; }
    public int TargetUserId { get; set; }
}