namespace MyBooks.Common.Dtos;

public class JwtClaimsDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public int AgeCategoryId { get; set; }
    public bool IsActive { get; set; }
    public bool AcceptedAup { get; set; }
}