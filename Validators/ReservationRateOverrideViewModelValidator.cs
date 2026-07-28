using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class ReservationRateOverrideViewModelValidator : IValidator<ReservationRateOverrideViewModel>
{
    public ValidationResult Validate(ReservationRateOverrideViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.UnitId is null or <= 0)
            errors.Add(new ValidationError("Birim seçilmelidir.", nameof(input.UnitId)));
        if (input.FreeDurationMinutes is < 0 or > 1440)
            errors.Add(new ValidationError(
                "Ücretsiz süre 0–1440 dakika arası olmalıdır.",
                nameof(input.FreeDurationMinutes)));
        if (input.BillingPeriodMinutes is < 1 or > 1440)
            errors.Add(new ValidationError(
                "Periyot süresi 0 olamaz. Lütfen geçerli bir periyot süresi giriniz.",
                nameof(input.BillingPeriodMinutes)));
        if (input.PeriodRate < 0)
            errors.Add(new ValidationError(
                "Tutar sıfır veya daha büyük olmalıdır.",
                nameof(input.PeriodRate)));
        if (input.KdvRate is < 0 or > 100)
            errors.Add(new ValidationError(
                "KDV oranı 0–100 arasında olmalıdır.",
                nameof(input.KdvRate)));
        if (input.Description?.Length > 300)
            errors.Add(new ValidationError(
                "Açıklama en fazla 300 karakter olabilir.",
                nameof(input.Description)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
