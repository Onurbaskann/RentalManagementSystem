using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Constants;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public class CreateLeaseViewModelValidator : IValidator<CreateLeaseViewModel>
{
    public ValidationResult Validate(CreateLeaseViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.SpaceId == null || input.SpaceId == 0)
            errors.Add(new ValidationError("Lütfen bir alan seçin.", nameof(input.SpaceId)));

        if (input.TenantId <= 0)
            errors.Add(new ValidationError("Lütfen bir kiracı seçin.", nameof(input.TenantId)));

        if (input.EndDate <= input.StartDate)
            errors.Add(new ValidationError("Bitiş tarihi başlangıç tarihinden büyük olmalıdır.", nameof(input.EndDate)));

        if (input.DueDateRuleType is not (DueDateRuleType.FixedDayOfMonth or DueDateRuleType.PeriodStartOffset))
            errors.Add(new ValidationError("Geçerli bir vade kuralı seçilmelidir.", nameof(input.DueDateRuleType)));

        if (input.DueDay < 1 || input.DueDay > 31)
            errors.Add(new ValidationError("Vade günü 1-31 arasında olmalıdır.", nameof(input.DueDay)));

        if (input.RentedArea < 0)
            errors.Add(new ValidationError("Kiralanan m² negatif olamaz.", nameof(input.RentedArea)));

        LeaseLineItemValidationRules.AddErrors(
            input.LeaseLineItems
                .Where(lineItem => !input.IsRentFree
                    || !string.Equals(lineItem.ChargeTypeCode, BorcTipiConsts.Kira, StringComparison.OrdinalIgnoreCase))
                .ToList(),
            nameof(input.LeaseLineItems),
            errors);

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
