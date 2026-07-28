using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class PropertyPricingQueryViewModelValidator : IValidator<PropertyPricingQueryViewModel>
{
    public ValidationResult Validate(PropertyPricingQueryViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.PropertyId <= 0)
            errors.Add(new ValidationError(
                "Geçerli bir taşınmaz seçilmelidir.",
                nameof(input.PropertyId)));
        if (input.Page < 1)
            errors.Add(new ValidationError(
                "Sayfa numarası 1 veya daha büyük olmalıdır.",
                nameof(input.Page)));
        if (input.PageSize is < 1 or > 100)
            errors.Add(new ValidationError(
                "Sayfa boyutu 1 ile 100 arasında olmalıdır.",
                nameof(input.PageSize)));

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors);
    }
}
