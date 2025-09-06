namespace MyBooks.TenantService.Dtos
{
    public class TenantCreateDto
    {
        public string Name { get; set; }
        public string Subdomain { get; set; }
        public int BillingPlanId { get; set; } = 1;
        public int OwnerUserId { get; set; }
    }
}
