using Microsoft.EntityFrameworkCore;
using MyBooks.Common.BaseClasses;
using MyBooks.FileService.Models;
using System.Security;
using System.Security.Claims;

namespace MyBooks.FileService.Data
{
    public class FileDbContext : DbContext
    {
        private readonly IHttpContextAccessor _contextAccessor;
        public FileDbContext(DbContextOptions<FileDbContext> options, IHttpContextAccessor contextAccessor) : base(options)
        {
            _contextAccessor = contextAccessor;
        }

        private string GetCurrentUserId()
        {
            var user = _contextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private int GetCurrentTenantId()
        {
            var tenantId = _contextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            Console.WriteLine($"Current Tenant ID: {tenantId}");
            return int.TryParse(tenantId, out var id) ? id : 0;
        }

        public DbSet<FileMetadata> Files { get; set; }
        public DbSet<ReadingProgress> ReadingProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Indexing for faster queries
            modelBuilder.Entity<FileMetadata>()
                .HasQueryFilter(f => f.IsActive && f.TenantId == GetCurrentTenantId())
                .HasIndex(f => f.BookId);

            modelBuilder.Entity<ReadingProgress>()
                .HasIndex(r => new { r.FileId, r.UserId });
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

        public void ApplyAuditInformation()
        {
            if(_contextAccessor.HttpContext == null)
                return;

            var currentUser = GetCurrentUserId();
            var currentTenant = GetCurrentTenantId();

            // apply tenant ID
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added && e.Entity.GetType().GetProperty("TenantId") != null))
            {
                entry.Entity.GetType().GetProperty("TenantId")?.SetValue(entry.Entity, currentTenant);
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
            var ipAddress = _contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            Console.WriteLine($"Current IP Address: {ipAddress}");

            foreach (var entry in ChangeTracker.Entries<FileMetadata>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (string.IsNullOrWhiteSpace(ipAddress))
                    {
                        throw new SecurityException("IP address is required.");
                    }
                    entry.Entity.UploadedByIp = ipAddress;
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
                        // only allow active and last modified fields to be updated
                        if (prop.Metadata.Name != nameof(FileMetadata.IsActive) &&
                            prop.Metadata.Name != nameof(FileMetadata.LastModifiedBy) &&
                            prop.Metadata.Name != nameof(FileMetadata.LastModifiedDate))
                        {
                            prop.IsModified = false;
                        }
                    }
                }
            }
        }
    }
}
