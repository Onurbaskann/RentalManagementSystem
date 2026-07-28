using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class ChangePasswordViewModelValidator : IValidator<ChangePasswordViewModel>
{
    public ValidationResult Validate(ChangePasswordViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.CurrentPassword))
            errors.Add(new ValidationError("Mevcut şifre zorunludur.", nameof(input.CurrentPassword)));

        if (string.IsNullOrWhiteSpace(input.NewPassword))
            errors.Add(new ValidationError("Yeni şifre zorunludur.", nameof(input.NewPassword)));
        else if (input.NewPassword.Length < 6)
            errors.Add(new ValidationError("Şifre en az 6 karakter olmalıdır.", nameof(input.NewPassword)));

        if (string.IsNullOrWhiteSpace(input.NewPasswordConfirm))
            errors.Add(new ValidationError("Şifre tekrarı zorunludur.", nameof(input.NewPasswordConfirm)));
        else if (input.NewPasswordConfirm != input.NewPassword)
            errors.Add(new ValidationError("Şifreler eşleşmiyor.", nameof(input.NewPasswordConfirm)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
