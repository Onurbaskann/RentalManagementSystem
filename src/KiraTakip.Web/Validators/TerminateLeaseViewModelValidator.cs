using KiraTakip.Infrastructure.Validation;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public class TerminateLeaseViewModelValidator : IValidator<TerminateLeaseViewModel>
{
    public ValidationResult Validate(TerminateLeaseViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.TerminationReason))
            errors.Add(new ValidationError("Fesih nedeni zorunludur.", nameof(input.TerminationReason)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
