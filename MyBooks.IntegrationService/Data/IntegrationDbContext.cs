using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyBooks.IntegrationService.Models;
using MyBooks.Common.BaseClasses;
using MyBooks.Common.Helpers;
using System.Security.Claims;

namespace MyBooks.IntegrationService.Data;

public class IntegrationDbContext : DbContext
{
    private readonly IHttpContextAccessor _contextAccessor;
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options, IHttpContextAccessor contextAccessor) : base(options)
    {
        _contextAccessor = contextAccessor;
    }

    public DbSet<Integration> Integrations { get; set; } = null!;
    public DbSet<ReadingCheckpoint> ReadingCheckpoints { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("integration");

        modelBuilder.Entity<Integration>(entity =>
        {
            entity.ToTable("Integrations");
            entity.HasQueryFilter(i => i.IsActive && i.TenantId == DbContextHelpers.GetCurrentTenantId(_contextAccessor));

            entity.Property(e => e.Provider)
                .HasConversion<int>() // store enum as int
                .IsRequired();

            entity.Property(e => e.ConfigJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ReadingCheckpoint>(entity =>
        {
            entity.ToTable("ReadingCheckpoints");
            entity.HasKey(e => e.Id);

            entity.HasQueryFilter(rc => rc.TenantId == DbContextHelpers.GetCurrentTenantId(_contextAccessor) && rc.UserId == DbContextHelpers.GetCurrentUserIdAsInt(_contextAccessor));
        });
    }

    public override int SaveChanges()
    {        
        DbContextHelpers.ApplyAuditInformation(_contextAccessor, ChangeTracker);
        SoftDelete();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {        
        DbContextHelpers.ApplyAuditInformation(_contextAccessor, ChangeTracker);
        SoftDelete();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public void SoftDelete()
    {
        foreach (var entry in ChangeTracker.Entries<Integration>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsActive = false;
            }
        }
    }
}