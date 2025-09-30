namespace MyBooks.Common.Dtos;

public class InviteDto
{
    public string ToEmail { get; set; }
    public string InvitedBy { get; set; }
    public string InvitationToken { get; set; }
}

public class AccountCreatedDto
{
    public string ToEmail { get; set; }
    public string FirstName { get; set; }
    public string InvitationToken { get; set; }
}