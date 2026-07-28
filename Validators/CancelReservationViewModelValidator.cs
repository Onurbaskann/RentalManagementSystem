using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class CancelReservationViewModelValidator : IValidator<CancelReservationViewModel>
{
    public ValidationResult Validate(CancelReservationViewModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Reason))
            return ValidationResult.Invalid(new ValidationError(
                "İptal nedeni zorunludur.",
                nameof(input.Reason)));

        return input.Reason.Trim().Length <= 450
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(new ValidationError(
                "İptal nedeni en fazla 450 karakter olabilir.",
                nameof(input.Reason)));
    }
}
