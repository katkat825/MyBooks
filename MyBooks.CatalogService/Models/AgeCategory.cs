namespace MyBooks.CatalogService.Models
{
    public class AgeCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Book> Books = new List<Book>();
    }
}
