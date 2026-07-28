using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class RoleCreateViewModelValidator : IValidator<RoleCreateViewModel>
{
    public ValidationResult Validate(RoleCreateViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add(new ValidationError("Rol adı zorunludur.", nameof(input.Name)));
        else if (input.Name.Length > 100)
            errors.Add(new ValidationError("Rol adı en fazla 100 karakter olabilir.", nameof(input.Name)));

        if (input.Description?.Length > 500)
            errors.Add(new ValidationError("Açıklama en fazla 500 karakter olabilir.", nameof(input.Description)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
