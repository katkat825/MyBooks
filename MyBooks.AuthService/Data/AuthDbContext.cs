using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using MyBooks.AuthService.Models;
using MyBooks.Common.BaseClasses;
using Microsoft.IdentityModel.Tokens;

namespace MyBooks.AuthService.Data
{
    public class AuthDbContext : DbContext
    {
        private readonly IHttpContextAccessor _contextAccessor;
        public AuthDbContext(DbContextOptions<AuthDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _contextAccessor = httpContextAccessor;
        }
        public DbSet<User> Users { get; set; }

        private string GetCurrentUserId()
        {
            var user = _contextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private int GetCurrentTenantId()
        {
            var tenantId = _contextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            return int.TryParse(tenantId, out var id) ? id : 0;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("auth");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Invitation>().ToTable("Invitations");

            modelBuilder.Entity<User>()
                .HasQueryFilter(u => u.TenantId == GetCurrentTenantId() && u.IsVisible);
        }

        public override int SaveChanges()
        {
            ApplyAuditInformation();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> SaveChangesAsSystemAsync(CancellationToken cancellationToken = default)
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
            
            return await base.SaveChangesAsync(cancellationToken);
        }

        public void ApplyAuditInformation()
        {
            if (_contextAccessor.HttpContext == null)
                return;

            var currentUser = GetCurrentUserId();
            var currentTenant = GetCurrentTenantId();

            if (string.IsNullOrWhiteSpace(currentUser))
                throw new InvalidOperationException("SaveChanges requires valid authenticated user.");

            if (currentTenant == 0)
                throw new InvalidOperationException("SaveChanges requires valid tenant.");

            // apply tenant ID
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added && e.Entity.GetType().GetProperty("TenantId") != null))
            {
                entry.Entity.GetType().GetProperty("TenantId")?.SetValue(entry.Entity, currentTenant);
            }

            // soft-delete only
            foreach (var entry in ChangeTracker.Entries<User>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsVisible = false;
                }
            }

            // apply audit information
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
    }
}
