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
            modelBuilder.HasDefaultSchema("tenant");
            modelBuilder.Entity<BillingPlan>().ToTable("BillingPlans");
            modelBuilder.Entity<Tenant>().ToTable("Tenants");

            modelBuilder.Entity<BillingPlan>(entity =>
            {
                entity.Property(p => p.MonthlyPrice).HasPrecision(18, 2);
                entity.Property(p => p.AnnualPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Tenant>(e =>
            {
                e.Property(p => p.CreditBalance).HasPrecision(18, 2);     // money-like
                e.Property(p => p.DiscountPercent).HasPrecision(5, 2);    // e.g., 0–100.00
            });

            modelBuilder.Entity<BillingPlan>().HasData(BillingPlanSeeder.GetSeedPlans());

            modelBuilder.Entity<Tenant>().HasData(new Tenant
                {
                    Id = 1,                // EF will insert this, even if the column is identity
                    Name = "Dev Tenant",
                    Subdomain = "dev",
                    BillingPlanId = 1,     // your pre-seeded plan
                    OwnerUserId = 1,       // placeholder, will match seeded admin
                    IsActive = true,
                    DiscountPercent = null,
                    CreditBalance = 0m,
                });

            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.Subdomain)
                .IsUnique();
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