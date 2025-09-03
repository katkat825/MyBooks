using MyBooks.Common.BaseClasses;

namespace MyBooks.TenantService.Models
{
    public static class BillingPlanSeeder
    {
        public static List<BillingPlan> GetSeedPlans()
        {
            return new List<BillingPlan>
            {
                new BillingPlan
                {
                    Id = 1,
                    Name = "Free",
                    MonthlyPrice = 0m,
                    AnnualPrice = 0m,
                    MaxStorageMb = 1_024, // 1 GB
                    IsActive = true
                },
                new BillingPlan
                {
                    Id = 2,
                    Name = "Basic",
                    MonthlyPrice = 4m,
                    AnnualPrice = 40m, // ~2 months free
                    MaxStorageMb = 5_120, // 5 GB
                    IsActive = true
                },
                new BillingPlan
                {
                    Id = 3,
                    Name = "Standard",
                    MonthlyPrice = 8m,
                    AnnualPrice = 80m,
                    MaxStorageMb = 15_360, // 15 GB
                    IsActive = true
                },
                new BillingPlan
                {
                    Id = 4,
                    Name = "Premium",
                    MonthlyPrice = 15m,
                    AnnualPrice = 150m,
                    MaxStorageMb = 51_200, // 50 GB
                    IsActive = true
                }
            };
        }
    }
}
