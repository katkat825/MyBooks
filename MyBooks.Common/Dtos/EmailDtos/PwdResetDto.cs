namespace MyBooks.Common.Dtos;

public class PwdResetDto
{
    public string ToEmail { get; set; }
    public string InvitationToken { get; set; }
}