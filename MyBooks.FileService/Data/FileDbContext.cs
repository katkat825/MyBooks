using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyBooks.Common.BaseClasses;
using MyBooks.FileService.Models;
using System.Security;
using System.Security.Claims;

namespace MyBooks.FileService.Data;

public class FileDbContext : DbContext
{
    private readonly IHttpContextAccessor _contextAccessor;

    public FileDbContext(DbContextOptions<FileDbContext> options, IHttpContextAccessor contextAccessor) : base(options)
    {
        _contextAccessor = contextAccessor;
    }

    public string GetCurrentUserId()
    {
        var user = _contextAccessor.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public int GetCurrentTenantId()
    {
        var tenantId = _contextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
        Console.WriteLine($"Current Tenant ID: {tenantId}");
        return int.TryParse(tenantId, out var id) ? id : 0;
    }

    public string GetCurrentUserRole()
    {
        var role = _contextAccessor.HttpContext?.User?.FindFirst("role")?.Value;
        return role ?? string.Empty;
    }

    public string GetCurrentUserIpAddress()
    {
        var context = _contextAccessor.HttpContext;
        if (context == null)
            return "unknown";
            
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();
        
        return _contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }

    public DbSet<FileMetadata> Files { get; set; }
    public DbSet<ReadingProgress> ReadingProgresses { get; set; }
    public DbSet<GoogleIntegration> GoogleIntegrations { get; set; }
    public DbSet<BulkImportJob> BulkImportJobs { get; set; }
    public DbSet<BulkImportItem> BulkImportItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("file");
        modelBuilder.Entity<FileMetadata>().ToTable("FilesMetaData");
        modelBuilder.Entity<ReadingProgress>().ToTable("ReadingProgress");
        modelBuilder.Entity<BulkImportJob>().ToTable("BulkImportJobs");
        modelBuilder.Entity<BulkImportItem>().ToTable("BulkImportItems");

        // Indexing for faster queries
        modelBuilder.Entity<FileMetadata>()
            .HasQueryFilter(f => f.IsActive && f.TenantId == GetCurrentTenantId())
            .HasIndex(f => f.BookId);

        modelBuilder.Entity<ReadingProgress>()
            .HasIndex(r => new { r.FileId, r.UserId });

        modelBuilder.Entity<GoogleIntegration>().ToTable("GoogleIntegrations");

        modelBuilder.Entity<GoogleIntegration>()
            .HasQueryFilter(g => g.IsActive && g.TenantId == GetCurrentTenantId());

        modelBuilder.Entity<GoogleIntegration>()
            .Property(g => g.DriveFolderIds)
            .HasConversion(
                d => d == null ? null : string.Join(',', d),
                d => string.IsNullOrEmpty(d) ? new List<string>() : d.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

        modelBuilder.Entity<FileMetadata>()
            .HasOne(f => f.GoogleIntegration)
            .WithMany() // not tracking files from the integration side
            .HasForeignKey(f => f.GoogleIntegrationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BulkImportJob>()
            .HasMany(j => j.Items)
            .WithOne(i => i.Job)
            .HasForeignKey(i => i.BulkImportJobId);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation();
        EnforceSecurityRules();
        
        return base.SaveChanges();
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        EnforceSecurityRules();    
                
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsSupportAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation(preserveSetTenant: true);
        EnforceSecurityRules();

        return await base.SaveChangesAsync(cancellationToken);
    }
    
    public async Task<int> SaveChangesAsSystemAsync(CancellationToken cancellationToken = default)
    {
        return await SaveChangesAsSystemAsync(null, null, cancellationToken);
    }

    public async Task<int> SaveChangesAsSystemAsync(string? userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        // verify audit info set by controller
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy) || entry.Entity.CreatedDate == default)
                {
                    throw new InvalidOperationException("System save requires CreatedBy and CreatedDate to be set.");
                }
            }
        }

        foreach (var entry in ChangeTracker.Entries<FileMetadata>())
        {
            EnforceSecurityRules(entry, userId, ipAddress);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public void ApplyAuditInformation(bool preserveSetTenant = false)
    {
        if (_contextAccessor.HttpContext == null)
            return;

        var currentUser = GetCurrentUserId();
        var currentTenant = GetCurrentTenantId();

        // apply tenant ID
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added && e.Entity.GetType().GetProperty("TenantId") != null))
        {
            var tenantProp = entry.Entity.GetType().GetProperty("TenantId");
            var existingTenantId = (int?)tenantProp?.GetValue(entry.Entity);
            
            if (!preserveSetTenant || existingTenantId == null || existingTenantId == 0)
            {
                entry.Entity.GetType().GetProperty("TenantId")?.SetValue(entry.Entity, currentTenant);
            }            
        }

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
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
    
    public void EnforceSecurityRules()
    {
        foreach (var entry in ChangeTracker.Entries<FileMetadata>())
        {
            EnforceSecurityRules(entry, null, null);
        }
    }

    public void EnforceSecurityRules(EntityEntry<FileMetadata> entry, string? userIdOverride, string? ipOverride)
    {
        var ipAddress = !string.IsNullOrWhiteSpace(ipOverride)
        ? ipOverride
        : GetCurrentUserIpAddress();

        var userId = !string.IsNullOrWhiteSpace(userIdOverride)
            ? userIdOverride
            : GetCurrentUserId();

        if (entry.State == EntityState.Added)
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || !System.Net.IPAddress.TryParse(ipAddress, out _))
            {
                throw new SecurityException("A valid IP address is required.");
            }

            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out _))
            {
                throw new SecurityException("A valid user ID is required");
            }

            entry.Entity.UploadedByIp = ipAddress;
            entry.Entity.CreatedBy = userId;
            entry.Entity.CreatedDate = DateTime.UtcNow;
        }
        else if (entry.State == EntityState.Deleted)
        {
            // soft delete only
            entry.Entity.IsActive = false;
            entry.State = EntityState.Modified;
        }
        else if (entry.State == EntityState.Modified)
        {
            foreach (var prop in entry.Properties)
            {
                // restrict updating to only fields allowed to be updated
                if (prop.Metadata.Name != nameof(FileMetadata.IsActive) &&
                    prop.Metadata.Name != nameof(FileMetadata.LastModifiedBy) &&
                    prop.Metadata.Name != nameof(FileMetadata.LastModifiedDate) &&
                    prop.Metadata.Name != nameof(FileMetadata.ConvertedFilePath) &&
                    prop.Metadata.Name != nameof(FileMetadata.IsConverted))
                {
                    prop.IsModified = false;
                }
            }
        }
    }
}
