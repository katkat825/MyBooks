using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyBooks.Common.BaseClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MyBooks.Common.Helpers;

public static class DbContextHelpers
{
    public static string GetCurrentUserId(IHttpContextAccessor accessor)
    {
        var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("No user context available");

        return userId;
    }

    public static int GetCurrentUserIdAsInt(IHttpContextAccessor accessor)
    {
        var userId = accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? accessor.HttpContext?.User?.FindFirst("sub")?.Value;

        return int.TryParse(userId, out var id) ? id : 0;
    }

    public static int GetCurrentTenantId(IHttpContextAccessor accessor)
    {
        var tenantId = accessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
        return int.TryParse(tenantId, out var id) ? id : 0;
    }

    public static async Task<int> SaveChangesAsSystemAsync(DbContext db, CancellationToken cancellationToken = default)
    {
        // verify audit info set by controller/service
        foreach (var entry in db.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy) || entry.Entity.CreatedDate == default)
                {
                    throw new InvalidOperationException(
                        "System save requires CreatedBy and CreatedDate to be set explicitly."
                    );
                }
            }
        }

        return await db.SaveChangesAsync(cancellationToken);
    }

    public static void ApplyAuditInformation(IHttpContextAccessor accessor, ChangeTracker changeTracker)
    {
        if (accessor.HttpContext == null)
            return;

        var currentUser = GetCurrentUserId(accessor);
        var currentTenant = GetCurrentTenantId(accessor);

        if (string.IsNullOrWhiteSpace(currentUser))
            throw new InvalidOperationException("SaveChanges requires valid authenticated user.");

        // apply tenant Id
        foreach (var entry in changeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity.GetType().GetProperty("TenantId") != null))
        {
            entry.Entity.GetType().GetProperty("TenantId")?.SetValue(entry.Entity, currentTenant);
        }

        // apply audit info
        foreach (var entry in changeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
                entry.Entity.CreatedBy = currentUser;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedDate = DateTime.UtcNow;
                entry.Entity.LastModifiedBy = currentUser;
            }
        }
    }
}