using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class SystemSettingEditViewModelValidator : IValidator<SystemSettingEditViewModel>
{
    public ValidationResult Validate(SystemSettingEditViewModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Value))
            return ValidationResult.Invalid(
                new ValidationError("Değer zorunludur.", nameof(input.Value)));

        if (input.Value.Length > 2000)
            return ValidationResult.Invalid(
                new ValidationError("Değer en fazla 2000 karakter olabilir.", nameof(input.Value)));

        return ValidationResult.Valid();
    }
}
