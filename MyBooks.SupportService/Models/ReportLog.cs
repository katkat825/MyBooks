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
    public string? TargetCreatedBy { get; set; } // userId of user who created book or uploaded file
    public DateTime DateReceived { get; set; }
    public DateTime? DateClosed { get; set; }
    public string? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
}

public static class StatusOptions
{
    public const string New = "New";
    public const string InReview = "In Review";
    public const string WaitingOnInfo = "Waiting on Info";
    public const string Closed = "Closed";
    public const string Reopened = "Reopened";
}

public static class ResolutionOptions
{
    public const string ItemRemoved = "Item Removed";
    public const string NoViolationFound = "No Violation Found";
    public const string DuplicateReport = "Duplicate Report";
    public const string InvalidReport = "Invalid Report";
}

public class UpdateReportLogDto
{
    public string? Status { get; set; }
    public string? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? DateClosed { get; set; }  // yyyy-MM-dd from Angular date input
    public string? TargetType { get; set; }
    public int? TargetId { get; set; }
    public string? TargetCreatedBy { get; set; }
}
