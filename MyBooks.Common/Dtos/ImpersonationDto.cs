namespace MyBooks.Common.Dtos;

public class ImpersonationDto
{
    public int TargetUserId { get; set; }
    public int ImpersonatingUserId { get; set; }
}