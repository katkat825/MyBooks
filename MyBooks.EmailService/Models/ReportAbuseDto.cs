namespace MyBooks.EmailService.Models
{
    public class ReportAbuseDto
    {
        public string Description { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
    }
}
