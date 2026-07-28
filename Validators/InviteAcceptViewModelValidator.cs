using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class InviteAcceptViewModelValidator : IValidator<InviteAcceptViewModel>
{
    public ValidationResult Validate(InviteAcceptViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.FullName))
            errors.Add(new ValidationError("Ad soyad zorunludur.", nameof(input.FullName)));

        if (string.IsNullOrWhiteSpace(input.Password))
            errors.Add(new ValidationError("Şifre zorunludur.", nameof(input.Password)));
        else if (input.Password.Length < 6)
            errors.Add(new ValidationError("Şifre en az 6 karakter olmalıdır.", nameof(input.Password)));

        if (input.PasswordConfirm != input.Password)
            errors.Add(new ValidationError("Şifreler eşleşmiyor.", nameof(input.PasswordConfirm)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
