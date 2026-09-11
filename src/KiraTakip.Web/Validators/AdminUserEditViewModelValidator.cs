using KiraTakip.Infrastructure.Validation;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public class AdminUserEditViewModelValidator : IValidator<AdminUserEditViewModel>
{
    public ValidationResult Validate(AdminUserEditViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.RoleId < 1)
            errors.Add(new ValidationError("Rol seçilmelidir.", nameof(input.RoleId)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
