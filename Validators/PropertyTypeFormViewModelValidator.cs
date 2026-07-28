using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class PropertyTypeFormViewModelValidator : IValidator<PropertyTypeFormViewModel>
{
    public ValidationResult Validate(PropertyTypeFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add(new ValidationError("Ad zorunludur.", nameof(input.Name)));
        else if (input.Name.Length > 100)
            errors.Add(new ValidationError("Ad en fazla 100 karakter olabilir.", nameof(input.Name)));

        if (!input.SupportsSingleUnit && !input.SupportsMultipleUnits)
            errors.Add(new ValidationError("En az bir birim yapısı seçilmelidir.", "birimYapisi"));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
