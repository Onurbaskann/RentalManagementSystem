using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Validators;

internal static class LeaseLineItemValidationRules
{
    public static void AddErrors(
        IReadOnlyList<LeaseLineItemInputDto> lineItems,
        string propertyName,
        ICollection<ValidationError> errors)
    {
        for (var index = 0; index < lineItems.Count; index++)
        {
            var lineItem = lineItems[index];
            var prefix = $"{propertyName}[{index}]";

            if (lineItem.ChargeTypeId <= 0)
                errors.Add(new ValidationError(
                    "Geçerli bir borç tipi seçilmelidir.",
                    $"{prefix}.{nameof(lineItem.ChargeTypeId)}"));
            if (lineItem.CalculationMethod is not (CalculationMethod.Fixed or CalculationMethod.M2))
                errors.Add(new ValidationError(
                    "Geçerli bir hesaplama yöntemi seçilmelidir.",
                    $"{prefix}.{nameof(lineItem.CalculationMethod)}"));
            if (lineItem.UnitValue < 0)
                errors.Add(new ValidationError(
                    "Birim değer 0'dan küçük olamaz.",
                    $"{prefix}.{nameof(lineItem.UnitValue)}"));
            if (lineItem.Amount < 0)
                errors.Add(new ValidationError(
                    "Tutar 0'dan küçük olamaz.",
                    $"{prefix}.{nameof(lineItem.Amount)}"));
            if (lineItem.VatRate is < 0 or > 100)
                errors.Add(new ValidationError(
                    "KDV oranı 0-100 arasında olmalıdır.",
                    $"{prefix}.{nameof(lineItem.VatRate)}"));
        }

        var duplicateChargeTypeIds = lineItems
            .Where(lineItem => lineItem.ChargeTypeId > 0)
            .GroupBy(lineItem => lineItem.ChargeTypeId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        for (var index = 0; index < lineItems.Count; index++)
        {
            if (duplicateChargeTypeIds.Contains(lineItems[index].ChargeTypeId))
                errors.Add(new ValidationError(
                    "Aynı borç tipi birden fazla kez gönderilemez.",
                    $"{propertyName}[{index}].{nameof(LeaseLineItemInputDto.ChargeTypeId)}"));
        }
    }
}
