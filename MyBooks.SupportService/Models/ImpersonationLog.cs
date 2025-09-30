namespace MyBooks.SupportService.Models;

public class ImpersonationLog
{
    public int Id { get; set; }
    public int TargetUserId { get; set; }  
    public int ImpersonatingUserId { get; set; } 
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}
