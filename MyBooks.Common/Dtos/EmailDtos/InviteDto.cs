namespace MyBooks.Common.Dtos;

public class InviteDto
{
    public string ToEmail { get; set; }
    public string InvitedBy { get; set; }
    public string InvitationToken { get; set; }
}