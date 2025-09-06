namespace MyBooks.Common.Dtos;

public class SignupRequestDto
{
    // tenant info
    public string TenantName { get; set; } = "tenant";
    public string Subdomain { get; set; }
    public int BillingPlanId { get; set; } = 1;

    // owner info
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}