using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class ReservationCreateViewModelValidator : IValidator<ReservationCreateViewModel>
{
    public ValidationResult Validate(ReservationCreateViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.UnitId is null or <= 0)
            errors.Add(new ValidationError("Taşınmaz birimi seçilmelidir.", nameof(input.UnitId)));

        if (input.TenantId is null or <= 0)
            errors.Add(new ValidationError("Kiracı seçilmelidir.", nameof(input.TenantId)));

        if (input.StartDate == default)
            errors.Add(new ValidationError("Başlangıç tarihi zorunludur.", nameof(input.StartDate)));

        if (input.EndDate == default)
            errors.Add(new ValidationError("Bitiş tarihi zorunludur.", nameof(input.EndDate)));
        else if (input.StartDate != default && input.EndDate <= input.StartDate)
            errors.Add(new ValidationError("Bitiş tarihi başlangıçtan sonra olmalıdır.", nameof(input.EndDate)));

        if (input.Description?.Length > 500)
            errors.Add(new ValidationError("Açıklama en fazla 500 karakter olabilir.", nameof(input.Description)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
