using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class RateMatrixPostViewModelValidator : IValidator<RateMatrixPostViewModel>
{
    public ValidationResult Validate(RateMatrixPostViewModel input)
    {
        var errors = new List<ValidationError>();

        for (var i = 0; i < input.Cells.Count; i++)
        {
            var cell = input.Cells[i];

            if (cell.CalculationMethod is not (CalculationMethod.Fixed or CalculationMethod.M2))
                errors.Add(new ValidationError(
                    "Geçerli bir hesaplama yöntemi seçilmelidir.",
                    $"{nameof(input.Cells)}[{i}].{nameof(cell.CalculationMethod)}"));
            if (cell.UnitValue < 0)
                errors.Add(new ValidationError(
                    "Birim değer 0'dan küçük olamaz.",
                    $"{nameof(input.Cells)}[{i}].{nameof(cell.UnitValue)}"));
            if (cell.KdvRate is < 0 or > 100)
                errors.Add(new ValidationError(
                    "KDV oranı 0-100 arasında olmalıdır.",
                    $"{nameof(input.Cells)}[{i}].{nameof(cell.KdvRate)}"));
        }

        for (var i = 0; i < input.ReservationCells.Count; i++)
        {
            var cell = input.ReservationCells[i];

            if (cell.FreeDurationMinutes < 0)
                errors.Add(new ValidationError(
                    "Ücretsiz süre 0'dan küçük olamaz.",
                    $"{nameof(input.ReservationCells)}[{i}].{nameof(cell.FreeDurationMinutes)}"));
            if (cell.BillingPeriodMinutes <= 0)
                errors.Add(new ValidationError(
                    "Ücretlendirme periyodu 0'dan büyük olmalıdır.",
                    $"{nameof(input.ReservationCells)}[{i}].{nameof(cell.BillingPeriodMinutes)}"));
            if (cell.PeriodRate < 0)
                errors.Add(new ValidationError(
                    "Periyot ücreti 0'dan küçük olamaz.",
                    $"{nameof(input.ReservationCells)}[{i}].{nameof(cell.PeriodRate)}"));
            if (cell.KdvRate is < 0 or > 100)
                errors.Add(new ValidationError(
                    "KDV oranı 0-100 arasında olmalıdır.",
                    $"{nameof(input.ReservationCells)}[{i}].{nameof(cell.KdvRate)}"));
        }

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
