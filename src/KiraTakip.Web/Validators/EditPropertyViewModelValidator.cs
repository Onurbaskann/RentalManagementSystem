using KiraTakip.Infrastructure.Validation;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public class EditPropertyViewModelValidator : IValidator<EditPropertyViewModel>
{
    public ValidationResult Validate(EditPropertyViewModel input)
    {
        var errors = new List<ValidationError>();
        PropertyValidationRules.AddErrors(input.ToInput(), errors);
        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
