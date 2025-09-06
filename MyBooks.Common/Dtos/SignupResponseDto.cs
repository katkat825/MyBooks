namespace MyBooks.Common.Dtos;

public class SignupResponseDto
{
    public int TenantId { get; set; }
    public int OwnerUserId { get; set; }
    public string PortalUrl { get; set; }
}
