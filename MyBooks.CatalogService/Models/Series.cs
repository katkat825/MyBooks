using MyBooks.Common.BaseClasses;

namespace MyBooks.CatalogService.Models
{
    public class Series : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }    
}