using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class TenantInvitationFormViewModelValidator : IValidator<TenantInvitationFormViewModel>
{
    public ValidationResult Validate(TenantInvitationFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Email))
            errors.Add(new ValidationError("E-posta zorunludur.", nameof(input.Email)));
        else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(input.Email))
            errors.Add(new ValidationError("Geçerli bir e-posta adresi giriniz.", nameof(input.Email)));
        else if (input.Email.Length > 256)
            errors.Add(new ValidationError("E-posta en fazla 256 karakter olabilir.", nameof(input.Email)));

        if (input.FullName?.Length > 200)
            errors.Add(new ValidationError("Ad soyad en fazla 200 karakter olabilir.", nameof(input.FullName)));

        if (input.RoleId < 1)
            errors.Add(new ValidationError("Rol seçilmelidir.", nameof(input.RoleId)));

        if (input.UnitIds.Any(unitId => unitId < 1)
            || input.UnitIds.Count != input.UnitIds.Distinct().Count())
            errors.Add(new ValidationError("Birim seçimi geçersizdir.", nameof(input.UnitIds)));

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors);
    }
}