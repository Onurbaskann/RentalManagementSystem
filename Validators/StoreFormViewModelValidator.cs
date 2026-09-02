using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class StoreFormViewModelValidator : IValidator<StoreFormViewModel>
{
    public ValidationResult Validate(StoreFormViewModel input)
    {
        var errors = new List<ValidationError>();
        var name = input.Name?.Trim() ?? string.Empty;

        if (name.Length == 0)
            errors.Add(new ValidationError("Ad zorunludur.", nameof(input.Name)));
        else if (name.Length > 200)
            errors.Add(new ValidationError("Ad en fazla 200 karakter olabilir.", nameof(input.Name)));

        if (input.Description?.Trim().Length > 500)
            errors.Add(new ValidationError("Açıklama en fazla 500 karakter olabilir.", nameof(input.Description)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
