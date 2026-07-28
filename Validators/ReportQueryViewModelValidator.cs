using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class ReportQueryViewModelValidator : IValidator<ReportQueryViewModel>
{
    public ValidationResult Validate(ReportQueryViewModel input)
    {
        if (input.Year is >= 2000 and <= 2100 || input.Year == null)
            return ValidationResult.Valid();

        return ValidationResult.Invalid(new ValidationError(
            "Yıl 2000 ile 2100 arasında olmalıdır.",
            nameof(input.Year)));
    }
}
