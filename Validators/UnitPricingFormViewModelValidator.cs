using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class UnitPricingFormViewModelValidator : IValidator<UnitPricingFormViewModel>
{
    public ValidationResult Validate(UnitPricingFormViewModel input)
    {
        var errors = new List<ValidationError>();
        var submittedPairs = new HashSet<(int TenantCategoryId, int ChargeTypeId)>();

        for (var rowIndex = 0; rowIndex < input.Rows.Count; rowIndex++)
        {
            var row = input.Rows[rowIndex];
            var rowPrefix = $"{nameof(input.Rows)}[{rowIndex}]";

            if (row.TenantCategoryId <= 0)
                errors.Add(new ValidationError(
                    "Geçerli bir kiracı kategorisi seçilmelidir.",
                    $"{rowPrefix}.{nameof(row.TenantCategoryId)}"));

            for (var cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
            {
                var cell = row.Cells[cellIndex];
                var cellPrefix = $"{rowPrefix}.{nameof(row.Cells)}[{cellIndex}]";

                if (cell.TenantCategoryId <= 0
                    || cell.ChargeTypeId <= 0
                    || cell.TenantCategoryId != row.TenantCategoryId
                    || !submittedPairs.Add((cell.TenantCategoryId, cell.ChargeTypeId)))
                    errors.Add(new ValidationError(
                        "Fiyat matrisinde geçersiz veya yinelenen bir hücre bulunuyor.",
                        cellPrefix));

                if (!cell.IsCustomRateActive) continue;

                if (cell.CalculationMethod is not (CalculationMethod.Fixed or CalculationMethod.M2))
                    errors.Add(new ValidationError(
                        "Geçerli bir hesaplama yöntemi seçilmelidir.",
                        $"{cellPrefix}.{nameof(cell.CalculationMethod)}"));
                if (cell.UnitValue < 0)
                    errors.Add(new ValidationError(
                        "Tutar sıfır veya daha büyük olmalıdır.",
                        $"{cellPrefix}.{nameof(cell.UnitValue)}"));
                if (cell.KdvRate is < 0 or > 100)
                    errors.Add(new ValidationError(
                        "KDV oranı 0-100 arasında olmalıdır.",
                        $"{cellPrefix}.{nameof(cell.KdvRate)}"));
            }
        }

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors);
    }
}
