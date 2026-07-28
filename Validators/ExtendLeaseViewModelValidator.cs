using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class ExtendLeaseViewModelValidator : IValidator<ExtendLeaseViewModel>
{
    public ValidationResult Validate(ExtendLeaseViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.NewEndDate == default)
            errors.Add(new ValidationError("Yeni bitiş tarihi zorunludur.", nameof(input.NewEndDate)));

        if (input.ApplyInflation && input.InflationRate.HasValue && input.InflationRate.Value < 0)
            errors.Add(new ValidationError("TÜFE oranı negatif olamaz.", nameof(input.InflationRate)));

        if (input.ApplyVat && input.VatRate is < 0 or > 100)
            errors.Add(new ValidationError("KDV oranı 0-100 arasında olmalıdır.", nameof(input.VatRate)));

        LeaseLineItemValidationRules.AddErrors(
            input.LeaseLineItems,
            nameof(input.LeaseLineItems),
            errors);

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
