using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Models;
using MyBooks.Common.BaseClasses;
using System.Security.Claims;

namespace MyBooks.CatalogService.Data
{
    public class CatalogDbContext : DbContext
    {
        private readonly IHttpContextAccessor _contextAccessor;
        
        private string GetCurrentUserId()
        {
            var user = _contextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public int GetCurrentTenantId()
        {
            var tenantId = _contextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            return int.TryParse(tenantId, out var id) ? id : 0;
        }

        public CatalogDbContext(DbContextOptions<CatalogDbContext> options, IHttpContextAccessor contextAccessor) : base(options)
        {
            _contextAccessor = contextAccessor;
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<AgeCategory> AgeCategories { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<MasterBook> MasterBooks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AgeCategory>().HasData(
                new AgeCategory { Id = 1, Name = "Children" },
                new AgeCategory { Id = 2, Name = "Young Adult" },
                new AgeCategory { Id = 3, Name = "Adult" }
            );

            modelBuilder.Entity<Series>()
                .HasQueryFilter(s => s.TenantId == GetCurrentTenantId() && s.IsActive);

            modelBuilder.Entity<Genre>()
                .HasQueryFilter(g => g.TenantId == GetCurrentTenantId() && g.IsActive)
                .HasData(
                    new Genre { Id = 1, Name = "Science Fiction", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 },
                    new Genre { Id = 2, Name = "Fantasy", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 },
                    new Genre { Id = 3, Name = "Mystery", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 },
                    new Genre { Id = 4, Name = "Romance", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 },
                    new Genre { Id = 5, Name = "Horror", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 }
                );

            modelBuilder.Entity<Tag>()
                .HasQueryFilter(t => t.TenantId == GetCurrentTenantId() && t.IsActive)
                .HasData(
                    new Tag { Id = 1, Name = "spicy", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 },
                    new Tag { Id = 2, Name = "magic", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 },
                    new Tag { Id = 3, Name = "detective", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 },
                    new Tag { Id = 4, Name = "love", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 },
                    new Tag { Id = 5, Name = "monsters", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25), TenantId = 1 }
                );

            modelBuilder.Entity<Book>()
                .HasQueryFilter(b => b.TenantId == GetCurrentTenantId() && b.IsActive)
                .HasData(
                    new Book
                    {
                        Id = 1,
                        Title = "Dune",
                        Author = "Frank Herbert",
                        Description = "A science fiction novel set in a distant future amidst a huge interstellar empire, where a young nobleman becomes embroiled in a complex struggle for control of the desert planet Arrakis.",
                        GenreId = 1,
                        AgeCategoryId = 3,
                        CreatedDate = new DateTime(2025, 2, 25),
                        CreatedBy = "system",
                        TenantId = 1
                    },
                new Book
                {
                    Id = 2,
                    Title = "The Hobbit",
                    Author = "J.R.R. Tolkien",
                    Description = "A fantasy novel that follows the journey of Bilbo Baggins, a hobbit who is swept into an epic quest to reclaim a treasure guarded by the dragon Smaug.",
                    GenreId = 2,
                    AgeCategoryId = 1,
                    CreatedDate = new DateTime(2025, 2, 25),
                    CreatedBy = "system",
                    TenantId = 1
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
                .Where(e => e.State == EntityState.Deleted || e.State == EntityState.Modified))
            {
                var entityType = entry.Entity.GetType();

                // prevent any modification of system-set entities
                if (entityType == typeof(AgeCategory) || entityType == typeof(MasterBook))
                {
                    throw new InvalidOperationException($"{entityType.Name} cannot be deleted or modified.");
                }

                // soft delete
                if (entry.State == EntityState.Deleted)
                {
                    var activeProperty = entry.Entity.GetType().GetProperty("IsActive");
                    if (activeProperty != null && activeProperty.PropertyType == typeof(bool))
                    {
                        entry.State = EntityState.Modified;
                        activeProperty?.SetValue(entry.Entity, false);
                    }
                }
            }
        }

        public void ApplyAuditInformation()
        {
            var currentUser = GetCurrentUserId();
            var currentTenant = GetCurrentTenantId();

            // apply tenant ID
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added && e.Entity.GetType().GetProperty("TenantId") != null))
            {
                entry.Entity.GetType().GetProperty("TenantId")?.SetValue(entry.Entity, currentTenant);
            }


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
