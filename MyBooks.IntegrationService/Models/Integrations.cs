using System.Text.Json;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Helpers;

namespace MyBooks.IntegrationService.Models;

public class Integration : AuditableEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public StorageProvider Provider { get; set; } = StorageProvider.Unknown;
    public string ConfigJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}