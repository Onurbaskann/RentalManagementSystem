using KiraTakip.Infrastructure.Validation;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public class TenantUserEditViewModelValidator : IValidator<TenantUserEditViewModel>
{
    public ValidationResult Validate(TenantUserEditViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.RoleId < 1)
            errors.Add(new ValidationError("Rol seçilmelidir.", nameof(input.RoleId)));

        if (input.SpaceIds.Count != input.SpaceIds.Distinct().Count())
            errors.Add(new ValidationError(
                "Aynı alan birden fazla seçilemez.",
                nameof(input.SpaceIds)));

        if (!input.HasAccessToAllSpaces && input.SpaceIds.Count == 0)
            errors.Add(new ValidationError(
                "Tüm alanlara erişim verilmeyecekse en az bir alan seçilmelidir.",
                nameof(input.SpaceIds)));

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors);
    }
}
