using Microsoft.EntityFrameworkCore;
using MyBooks.Common.BaseClasses;
using MyBooks.FileService.Models;
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

        public DbSet<FileMetadata> Files { get; set; }
        public DbSet<ReadingProgress> ReadingProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Indexing for faster queries
            modelBuilder.Entity<FileMetadata>()
                .HasIndex(f => f.BookId); 

            modelBuilder.Entity<ReadingProgress>()
                .HasIndex(r => new {r.FileId, r.UserId});
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

        public void ApplyAuditInformation()
        {
            var currentUser = _contextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUser))
            {
                currentUser = "system";
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
    }
}
