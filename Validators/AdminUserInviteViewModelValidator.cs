using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class AdminUserInviteViewModelValidator : IValidator<AdminUserInviteViewModel>
{
    public ValidationResult Validate(AdminUserInviteViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Email))
            errors.Add(new ValidationError("E-posta adresi zorunludur.", nameof(input.Email)));
        else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(input.Email))
            errors.Add(new ValidationError("Geçerli bir e-posta adresi girin.", nameof(input.Email)));

        if (input.RoleId < 1)
            errors.Add(new ValidationError("Rol seçimi zorunludur.", nameof(input.RoleId)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
