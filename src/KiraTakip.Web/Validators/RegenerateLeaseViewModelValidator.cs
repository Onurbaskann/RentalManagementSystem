using KiraTakip.Infrastructure.Validation;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public class RegenerateLeaseViewModelValidator : IValidator<RegenerateLeaseViewModel>
{
    public ValidationResult Validate(RegenerateLeaseViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.StartDate == default)
            errors.Add(new ValidationError("Başlangıç tarihi zorunludur.", nameof(input.StartDate)));

        LeaseLineItemValidationRules.AddErrors(
            input.LeaseLineItems ?? [],
            nameof(input.LeaseLineItems),
            errors);

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
