using MyBooks.Common.BaseClasses;

namespace MyBooks.CatalogService.Models
{
    public class Tag : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<Book> Books = new List<Book>();
    }
}
