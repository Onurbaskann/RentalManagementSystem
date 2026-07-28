using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class UnitTypeFormViewModelValidator : IValidator<UnitTypeFormViewModel>
{
    public ValidationResult Validate(UnitTypeFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add(new ValidationError("Ad zorunludur.", nameof(input.Name)));
        else if (input.Name.Length > 100)
            errors.Add(new ValidationError("Ad en fazla 100 karakter olabilir.", nameof(input.Name)));

        if (input.SortOrder < 1)
            errors.Add(new ValidationError("Sıra en az 1 olmalıdır.", nameof(input.SortOrder)));

        if (!Enum.IsDefined(input.Usage))
            errors.Add(new ValidationError("Geçerli bir kullanım türü seçilmelidir.", nameof(input.Usage)));

        if (input.Usage == UnitTypeUsage.Reservable &&
            (!input.ChargeTypeId.HasValue || input.ChargeTypeId <= 0))
        {
            errors.Add(new ValidationError(
                "Rezervasyon birim türü için borç tipi seçilmelidir.",
                nameof(input.ChargeTypeId)));
        }

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
