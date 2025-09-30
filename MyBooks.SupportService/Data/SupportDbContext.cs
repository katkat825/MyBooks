using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyBooks.Common.BaseClasses;
using MyBooks.SupportService.Models;
using System.Security.Claims;

namespace MyBooks.SupportService.Data;

public class SupportDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SupportDbContext(DbContextOptions<SupportDbContext> options, IHttpContextAccessor accessor)
        : base(options)
    {
        _httpContextAccessor = accessor;
    }

    public DbSet<ReportLog> ReportLogs { get; set; }
    public DbSet<ImpersonationLog> ImpersonationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("support");
        modelBuilder.Entity<ReportLog>().ToTable("ReportLogs");
        modelBuilder.Entity<ImpersonationLog>().ToTable("ImpersonationLogs");
    }

    public override int SaveChanges()
    {
        PreventInvalidChanges(ChangeTracker);
        ApplyAuditInformation(ChangeTracker);
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PreventInvalidChanges(ChangeTracker);
        ApplyAuditInformation(ChangeTracker);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void PreventInvalidChanges(ChangeTracker changeTracker)
    {
        // prevent deletes of any items
        foreach (var entry in changeTracker.Entries())
        {
            if (entry.State == EntityState.Deleted)
            {
                throw new InvalidOperationException("Delete operation not allowed");
            }
        }

        // prevent updating impersonation log except endtime
        foreach (var entry in changeTracker.Entries<ImpersonationLog>())
        {
            if (entry.State == EntityState.Modified)
            {
                // allow only EndTime to change
                foreach (var prop in entry.Properties)
                {
                    if (prop.IsModified && prop.Metadata.Name != nameof(ImpersonationLog.EndTime))
                    {
                        throw new InvalidOperationException("Only EndTime can be modified for ImpersonationLog records.");
                    }
                }
            }
        }
    }

    private void ApplyAuditInformation(ChangeTracker changeTracker)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("SaveChanges requires valid authenticated user.");

        foreach (var entry in changeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
                entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedDate = DateTime.UtcNow;
                entry.Entity.LastModifiedBy = userId;
            }
        }
    }
}