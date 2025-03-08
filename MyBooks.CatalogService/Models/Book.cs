using MyBooks.Common.BaseClasses;
using System.Text.Json.Serialization;

namespace MyBooks.CatalogService.Models
{
    public class Book : AuditableEntity
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Author { get; set; }
        public int? SeriesId { get; set; }
        public Series? Series { get; set; }
        public int? SeriesPosition { get; set; }
        public string? Description { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? ISBN { get; set; }
        public string? Location { get; set; }
        public int GenreId { get; set; }
        public Genre? Genre { get; set; }
        public int AgeCategoryId { get; set; }
        public AgeCategory? AgeCategory { get; set; }
        public string? TagInput { get; set; }
        public ICollection<Tag>? Tags { get; set; }
    }
}