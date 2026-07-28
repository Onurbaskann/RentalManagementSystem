using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class CategoryFormViewModelValidator : IValidator<CategoryFormViewModel>
{
    public ValidationResult Validate(CategoryFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add(new ValidationError("Ad zorunludur.", nameof(input.Name)));
        else if (input.Name.Length > 100)
            errors.Add(new ValidationError("Ad en fazla 100 karakter olabilir.", nameof(input.Name)));

        if (input.Order < 1)
            errors.Add(new ValidationError("Sıra en az 1 olmalıdır.", nameof(input.Order)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
