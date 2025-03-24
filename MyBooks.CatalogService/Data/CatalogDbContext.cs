using Microsoft.EntityFrameworkCore;
using MyBooks.CatalogService.Models;
using MyBooks.Common.BaseClasses;
using System.Security.Claims;

namespace MyBooks.CatalogService.Data
{
    public class CatalogDbContext : DbContext
    {
        private readonly IHttpContextAccessor _contextAccessor;
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options, IHttpContextAccessor contextAccessor) : base(options) 
        {
            _contextAccessor = contextAccessor;
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<AgeCategory> AgeCategories { get; set; }
        public DbSet<Series> Series { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AgeCategory>().HasData(
                new AgeCategory { Id = 1, Name = "Children" },
                new AgeCategory { Id = 2, Name = "Young Adult" },
                new AgeCategory { Id = 3, Name = "Adult" }
            );
            modelBuilder.Entity<Genre>().HasData(
                new Genre { Id = 1, Name = "Science Fiction", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) },
                new Genre { Id = 2, Name = "Fantasy", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) },
                new Genre { Id = 3, Name = "Mystery", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) },
                new Genre { Id = 4, Name = "Romance", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) },
                new Genre { Id = 5, Name = "Horror", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) }
            );
            modelBuilder.Entity<Tag>().HasData(
                new Tag { Id = 1, Name = "spicy", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) },
                new Tag { Id = 2, Name = "magic", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) },
                new Tag { Id = 3, Name = "detective", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) },
                new Tag { Id = 4, Name = "love", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) },
                new Tag { Id = 5, Name = "monsters", CreatedBy = "system", CreatedDate = new DateTime(2025, 2, 25) }
            );
            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    Id = 1,
                    Title = "Dune",
                    Author = "Frank Herbert",
                    Description = "A science fiction novel set in a distant future amidst a huge interstellar empire, where a young nobleman becomes embroiled in a complex struggle for control of the desert planet Arrakis.",
                    GenreId = 1,
                    AgeCategoryId = 3,
                    CreatedDate = new DateTime(2025, 2, 25),
                    CreatedBy = "system"
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
                    CreatedBy = "system"
                }
            );
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
