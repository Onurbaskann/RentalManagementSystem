using KiraTakip.Infrastructure.Validation;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public class RateYearAddViewModelValidator : IValidator<RateYearAddViewModel>
{
    public ValidationResult Validate(RateYearAddViewModel input)
    {
        if (input.Year is < 2000 or > 2100)
            return ValidationResult.Invalid(new ValidationError(
                "Yıl 2000-2100 arasında olmalıdır.",
                nameof(input.Year)));

        return ValidationResult.Valid();
    }
}
