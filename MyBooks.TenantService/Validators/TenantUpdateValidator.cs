using FluentValidation;
using MyBooks.TenantService.Dtos;

namespace MyBooks.TenantService.Validators
{
    public class TenantUpdateValidator : AbstractValidator<TenantUpdateDto>
    {
        public TenantUpdateValidator()
        {
            RuleFor(t => t.BillingPlanId)
                .GreaterThan(0).WithMessage("A valid billing plan is required.");
        }
    }
}