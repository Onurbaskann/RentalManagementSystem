using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class TenantUserEditViewModelValidator : IValidator<TenantUserEditViewModel>
{
    public ValidationResult Validate(TenantUserEditViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.RoleId < 1)
            errors.Add(new ValidationError("Rol seçilmelidir.", nameof(input.RoleId)));

        if (input.UnitIds.Count != input.UnitIds.Distinct().Count())
            errors.Add(new ValidationError(
                "Aynı birim birden fazla seçilemez.",
                nameof(input.UnitIds)));

        if (!input.HasAccessToAllUnits && input.UnitIds.Count == 0)
            errors.Add(new ValidationError(
                "Tüm birimlere erişim verilmeyecekse en az bir birim seçilmelidir.",
                nameof(input.UnitIds)));

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors);
    }
}
