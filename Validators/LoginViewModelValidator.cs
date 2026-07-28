using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class LoginViewModelValidator : IValidator<LoginViewModel>
{
    private static readonly System.ComponentModel.DataAnnotations.EmailAddressAttribute EmailFormat = new();

    public ValidationResult Validate(LoginViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Email))
            errors.Add(new ValidationError("E-posta adresi zorunludur.", nameof(input.Email)));
        else if (!EmailFormat.IsValid(input.Email))
            errors.Add(new ValidationError("Geçerli bir e-posta adresi girin.", nameof(input.Email)));

        if (string.IsNullOrWhiteSpace(input.Password))
            errors.Add(new ValidationError("Şifre zorunludur.", nameof(input.Password)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
