using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MyBooks.Common.BaseClasses;
using MyBooks.TenantService.Models;
using System.Security;
using System.Security.Claims;

namespace MyBooks.TenantService.Data
{
    public class TenantDbContext : DbContext
    {
        private readonly IHttpContextAccessor _contextAccessor;
        public TenantDbContext(DbContextOptions<TenantDbContext> options, IHttpContextAccessor contextAccessor)
            : base(options)
        {
            _contextAccessor = contextAccessor;
        }

        private string GetCurrentUserId()
        {
            var user = _contextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<BillingPlan> BillingPlans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BillingPlan>(entity =>
            {
                entity.Property(p => p.MonthlyPrice).HasPrecision(18, 2);
                entity.Property(p => p.AnnualPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.Subdomain)
                .IsUnique();

            modelBuilder.Entity<BillingPlan>()
                .HasData(
                    new BillingPlan
                    {
                        Id = 1,
                        Name = "Dev Testing",
                        MonthlyPrice = 0,
                        AnnualPrice = 0,
                        MaxUsers = 0,
                        MaxStorageMb = 0,
                        AllowExternalIntegrations = true,
                        AllowStorage = true,
                        IsActive = true
                    }
                );

            modelBuilder.Entity<Tenant>()
                .HasData(
                    new Tenant
                    {
                        Id = 1,
                        Name = "Tenant One",
                        Subdomain = "tenant1",
                        BillingPlanId = 1,
                        OwnerUserId = 3,
                        IsActive = true,
                        CreatedDate = new DateTime(2025, 01, 01),
                        CreatedBy = "System"
                    },
                    new Tenant
                    {
                        Id = 2,
                        Name = "Tenant Two",
                        Subdomain = "tenant2",
                        BillingPlanId = 1,
                        OwnerUserId = 4,
                        IsActive = true,
                        CreatedDate = new DateTime(2025, 01, 01),
                        CreatedBy = "System"
                    }
                );
        }

        public override int SaveChanges()
        {
            SoftDelete();
            ApplyAuditInformation();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SoftDelete();
            ApplyAuditInformation();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public void SoftDelete()
        {
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Deleted))
            {
                entry.State = EntityState.Modified;
                entry.Entity.GetType().GetProperty("IsActive")?.SetValue(entry.Entity, false);
            }
        }

        public void ApplyAuditInformation()
        {
            var currentUser = GetCurrentUserId();

            if (string.IsNullOrEmpty(currentUser))
            {
                currentUser = "System";
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