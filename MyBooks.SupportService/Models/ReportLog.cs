using MyBooks.Common.BaseClasses;

namespace MyBooks.SupportService.Models;

public class ReportLog : AuditableEntity
{
    public int Id { get; set; }
    public string ReportedBy { get; set; } // userId or email
    public string ReportType { get; set; } // e.g. "DMCA", "Abuse"
    public string Status { get; set; } = "Open"; // e.g. "Open", "In Review", "Closed"
    public string Description { get; set; } // details of the report
    public string? TargetType { get; set; } // e.g. "Book", "File"
    public int? TargetId { get; set; }
    public DateTime DateReceived { get; set; }
    public DateTime? DateClosed { get; set; }
    public string? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
}

public static class StatusOptions
{
    public const string Open = "Open";
    public const string InReview = "In Review";
    public const string Closed = "Closed";
}

public static class ResolutionOptions
{
    public const string ItemRemoved = "Item Removed";
    public const string NoViolationFound = "No Violation Found";
    public const string DuplicateReport = "Duplicate Report";
}
