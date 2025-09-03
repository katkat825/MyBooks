using MyBooks.Common.BaseClasses;

namespace MyBooks.TenantService.Models
{
    public class BillingPlan : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public decimal MonthlyPrice { get; set; }
        public decimal? AnnualPrice { get; set; }

        public int MaxStorageMb { get; set; } 
        
        public bool IsActive { get; set; } = true;
    }
}
