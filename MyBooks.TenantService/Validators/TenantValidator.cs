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

            RuleFor(t => t.Name)
                .NotEmpty().WithMessage("Tenant name is required.")
                .MaximumLength(100).WithMessage("Tenant name cannot exceed 100 characters.");

            RuleFor(t => t.Subdomain)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Subdomain is required.")
                .MaximumLength(50).WithMessage("Subdomain cannot exceed 50 characters.")
                .Matches("^[a-z0-9-]+$").WithMessage("Subdomain can only contain lowercase letters, numbers, and dashes.")
                .Must(s => !s.StartsWith("-") && !s.EndsWith("-"))
                    .WithMessage("Subdomain cannot start or end with a dash.")
                .MustAsync(async (subdomain, cancellation) =>
                {
                    return !await _dbContext.Tenants.AnyAsync(t => t.Subdomain == subdomain, cancellation);
                }).WithMessage("Subdomain is already in use.");

            RuleFor(t => t.BillingPlanId)
                .GreaterThan(0).WithMessage("Billing plan is required.");

            RuleFor(t => t.OwnerUserId)
                .GreaterThan(0).WithMessage("A valid owner user ID is required.");
        }
    }
}