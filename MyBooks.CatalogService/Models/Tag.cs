using MyBooks.Common.BaseClasses;
using System.Text.Json.Serialization;

namespace MyBooks.CatalogService.Models
{
    public class Tag : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int TenantId { get; set; }

        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
