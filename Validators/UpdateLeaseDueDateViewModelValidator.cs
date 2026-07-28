using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class UpdateLeaseDueDateViewModelValidator : IValidator<UpdateLeaseDueDateViewModel>
{
    public ValidationResult Validate(UpdateLeaseDueDateViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.RuleType is not (DueDateRuleType.FixedDayOfMonth or DueDateRuleType.PeriodStartOffset))
            errors.Add(new ValidationError("Geçerli bir vade kuralı seçilmelidir.", nameof(input.RuleType)));

        if (input.DueDay < 1 || input.DueDay > 31)
            errors.Add(new ValidationError("Vade günü 1-31 arasında olmalıdır.", nameof(input.DueDay)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
