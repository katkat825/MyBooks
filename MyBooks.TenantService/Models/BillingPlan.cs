using MyBooks.Common.BaseClasses;

namespace MyBooks.TenantService.Models
{
    public class BillingPlan : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public decimal MonthlyPrice { get; set; }
        public decimal? AnnualPrice { get; set; }   

        public int MaxUsers { get; set; } // 0 = unlimited
        public int MaxStorageMb { get; set; } // 0 = unlimited
        public bool AllowStorage { get; set; } = false;
        public bool AllowExternalIntegrations { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }
}
