using KiraTakip.Authorization;
using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class TenantRoleFormViewModelValidator : IValidator<TenantRoleFormViewModel>
{
    public ValidationResult Validate(TenantRoleFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add(new ValidationError("Rol adı zorunludur.", nameof(input.Name)));
        else if (input.Name.Length > 100)
            errors.Add(new ValidationError("Rol adı en fazla 100 karakter olabilir.", nameof(input.Name)));

        if (input.Description?.Length > 500)
            errors.Add(new ValidationError("Açıklama en fazla 500 karakter olabilir.", nameof(input.Description)));

        if (input.SelectedPermissions.Any(permission => !PermissionCatalog.TenantAll.Contains(permission)))
            errors.Add(new ValidationError("Geçersiz izin seçimi.", nameof(input.SelectedPermissions)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
