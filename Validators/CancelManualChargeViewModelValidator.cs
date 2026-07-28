using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class CancelManualChargeViewModelValidator : IValidator<CancelManualChargeViewModel>
{
    public ValidationResult Validate(CancelManualChargeViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Reason))
            errors.Add(new ValidationError("İptal nedeni zorunludur.", nameof(input.Reason)));
        else if (input.Reason.Length > 500)
            errors.Add(new ValidationError("İptal nedeni en fazla 500 karakter olabilir.", nameof(input.Reason)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
