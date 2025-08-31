using MyBooks.Common.BaseClasses;

namespace MyBooks.TenantService.Models
{
    public class Tenant : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subdomain { get; set; }
        public int BillingPlanId { get; set; }
        public BillingPlan BillingPlan { get; set; }
        public int OwnerUserId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}