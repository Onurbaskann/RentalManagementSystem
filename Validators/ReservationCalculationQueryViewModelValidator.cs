using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class ReservationCalculationQueryViewModelValidator
    : IValidator<ReservationCalculationQueryViewModel>
{
    public ValidationResult Validate(ReservationCalculationQueryViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.UnitId <= 0)
            errors.Add(new ValidationError("Birim bilgisi geçersizdir.", nameof(input.UnitId)));

        var hasValidStart = DateTime.TryParse(input.Start, out var startDate);
        var hasValidEnd = DateTime.TryParse(input.End, out var endDate);

        if (!hasValidStart)
            errors.Add(new ValidationError("Başlangıç tarihi geçersizdir.", nameof(input.Start)));

        if (!hasValidEnd)
            errors.Add(new ValidationError("Bitiş tarihi geçersizdir.", nameof(input.End)));
        else if (hasValidStart && endDate <= startDate)
            errors.Add(new ValidationError("Bitiş tarihi başlangıçtan sonra olmalıdır.", nameof(input.End)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
