namespace MyBooks.EmailService.Models
{
    public class ReportAbuseDto
    {
        public string PageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
    }
}
