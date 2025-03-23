namespace MyBooks.FileService.Models
{
    public class ReadingProgress
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public int UserId { get; set; }
        public double ProgressPercent { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
