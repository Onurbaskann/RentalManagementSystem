using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class CalculateRentIncreaseViewModelValidator : IValidator<CalculateRentIncreaseViewModel>
{
    public ValidationResult Validate(CalculateRentIncreaseViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.CurrentAmount < 0)
            errors.Add(new ValidationError("Mevcut tutar negatif olamaz.", nameof(input.CurrentAmount)));
        if (input.InflationRate < 0)
            errors.Add(new ValidationError("TÜFE oranı negatif olamaz.", nameof(input.InflationRate)));
        if (input.ApplyVat && input.VatRate is < 0 or > 100)
            errors.Add(new ValidationError("KDV oranı 0-100 arasında olmalıdır.", nameof(input.VatRate)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
