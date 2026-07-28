using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class CreateLeaseViewModelValidator : IValidator<CreateLeaseViewModel>
{
    public ValidationResult Validate(CreateLeaseViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.UnitId == null || input.UnitId == 0)
            errors.Add(new ValidationError("Lütfen bir birim seçin.", nameof(input.UnitId)));

        if (input.TenantId <= 0)
            errors.Add(new ValidationError("Lütfen bir kiracı seçin.", nameof(input.TenantId)));

        if (input.EndDate <= input.StartDate)
            errors.Add(new ValidationError("Bitiş tarihi başlangıç tarihinden büyük olmalıdır.", nameof(input.EndDate)));

        if (input.DueDateRuleType is not (DueDateRuleType.FixedDayOfMonth or DueDateRuleType.PeriodStartOffset))
            errors.Add(new ValidationError("Geçerli bir vade kuralı seçilmelidir.", nameof(input.DueDateRuleType)));

        if (input.DueDay < 1 || input.DueDay > 31)
            errors.Add(new ValidationError("Vade günü 1-31 arasında olmalıdır.", nameof(input.DueDay)));

        LeaseLineItemValidationRules.AddErrors(
            input.LeaseLineItems,
            nameof(input.LeaseLineItems),
            errors);

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
