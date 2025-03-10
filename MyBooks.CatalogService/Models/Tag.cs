using MyBooks.Common.BaseClasses;
using System.Text.Json.Serialization;

namespace MyBooks.CatalogService.Models
{
    public class Tag : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
