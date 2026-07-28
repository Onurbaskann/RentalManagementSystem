using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class ForgotPasswordViewModelValidator : IValidator<ForgotPasswordViewModel>
{
    private static readonly System.ComponentModel.DataAnnotations.EmailAddressAttribute EmailFormat = new();

    public ValidationResult Validate(ForgotPasswordViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Email))
            errors.Add(new ValidationError("E-posta adresi zorunludur.", nameof(input.Email)));
        else if (!EmailFormat.IsValid(input.Email))
            errors.Add(new ValidationError("Geçerli bir e-posta adresi girin.", nameof(input.Email)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
