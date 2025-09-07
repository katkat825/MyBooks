using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MyBooks.TenantService.Data;
using MyBooks.TenantService.Models;

namespace MyBooks.TenantService.Validators
{
    public class TenantValidator : AbstractValidator<Tenant>
    {
        private readonly TenantDbContext _dbContext;

        public TenantValidator(TenantDbContext dbContext)
        {
            _dbContext = dbContext;

            RuleFor(t => t.BillingPlanId)
                .GreaterThan(0).WithMessage("Billing plan is required.");

            RuleFor(t => t.OwnerUserId)
                .GreaterThan(0).WithMessage("A valid owner user ID is required.");
        }
    }
}