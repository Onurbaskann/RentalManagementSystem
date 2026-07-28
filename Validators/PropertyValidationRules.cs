using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Validators;

internal static class PropertyValidationRules
{
    public static void AddErrors(CreatePropertyInput input, List<ValidationError> errors)
    {
        Required(input.Name, "Taşınmaz adı zorunludur.", nameof(input.Name), errors);
        MaxLength(input.Name, 200, "Taşınmaz adı en fazla 200 karakter olabilir.", nameof(input.Name), errors);
        if (input.PropertyTypeId is null or <= 0)
            errors.Add(new ValidationError("Taşınmaz tipi zorunludur.", nameof(input.PropertyTypeId)));
        if (!Enum.IsDefined(input.UnitStructure))
            errors.Add(new ValidationError("Geçerli bir birim yapısı seçiniz.", nameof(input.UnitStructure)));

        Required(input.City, "İl zorunludur.", nameof(input.City), errors);
        MaxLength(input.City, 100, "İl en fazla 100 karakter olabilir.", nameof(input.City), errors);
        Required(input.District, "İlçe zorunludur.", nameof(input.District), errors);
        MaxLength(input.District, 100, "İlçe en fazla 100 karakter olabilir.", nameof(input.District), errors);
        Required(input.Neighborhood, "Mahalle zorunludur.", nameof(input.Neighborhood), errors);
        MaxLength(input.Neighborhood, 200, "Mahalle en fazla 200 karakter olabilir.", nameof(input.Neighborhood), errors);
        Required(input.Address, "Açık adres zorunludur.", nameof(input.Address), errors);
        MaxLength(input.Address, 500, "Açık adres en fazla 500 karakter olabilir.", nameof(input.Address), errors);

        if (input.OpenArea < 0)
            errors.Add(new ValidationError("Açık alan negatif olamaz.", nameof(input.OpenArea)));
        if (input.ClosedArea < 0)
            errors.Add(new ValidationError("Kapalı alan negatif olamaz.", nameof(input.ClosedArea)));
        if (input.FloorCount < 0)
            errors.Add(new ValidationError("Kat sayısı negatif olamaz.", nameof(input.FloorCount)));

        if (input.UnitStructure == UnitStructure.SingleUnit)
        {
            if (input.SingleUnitTypeId is null or <= 0)
                errors.Add(new ValidationError("Tek birim yapısı için birim türü zorunludur.", nameof(input.SingleUnitTypeId)));
        }
        else if (input.UnitStructure == UnitStructure.MultipleUnits)
        {
            if (input.Units.Count + input.ReservationAreas.Count == 0)
                errors.Add(new ValidationError("Çoklu birim yapısı için en az bir birim eklemelisiniz.", nameof(input.Units)));

            ValidateUnits(input.Units, errors);
            ValidateReservationAreas(input.ReservationAreas, errors);
            ValidateDuplicateUnitNumbers(input, errors);
            ValidateDuplicateIds(input, errors);
        }

        ValidatePricing(input.PricingMatrix, errors);
    }

    private static void ValidateUnits(IReadOnlyList<PropertyUnitInputDto> units, List<ValidationError> errors)
    {
        for (var i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            var prefix = $"Units[{i}]";
            Required(unit.UnitNo, "Birim No zorunludur.", $"{prefix}.{nameof(unit.UnitNo)}", errors);
            MaxLength(unit.UnitNo, 50, "Birim No en fazla 50 karakter olabilir.", $"{prefix}.{nameof(unit.UnitNo)}", errors);
            if (unit.FloorNo == null)
                errors.Add(new ValidationError("Kat No zorunludur.", $"{prefix}.{nameof(unit.FloorNo)}"));
            if (unit.UnitTypeId is null or <= 0)
                errors.Add(new ValidationError("Birim Türü zorunludur.", $"{prefix}.{nameof(unit.UnitTypeId)}"));
            Required(unit.Name, "Ad zorunludur.", $"{prefix}.{nameof(unit.Name)}", errors);
            MaxLength(unit.Name, 200, "Ad en fazla 200 karakter olabilir.", $"{prefix}.{nameof(unit.Name)}", errors);
            if (unit.Area <= 0)
                errors.Add(new ValidationError("Yüzölçümü 0'dan büyük olmalıdır.", $"{prefix}.{nameof(unit.Area)}"));
        }
    }

    private static void ValidateReservationAreas(IReadOnlyList<ReservationAreaInputDto> areas, List<ValidationError> errors)
    {
        for (var i = 0; i < areas.Count; i++)
        {
            var area = areas[i];
            var prefix = $"ReservationAreas[{i}]";
            Required(area.UnitNo, "Birim No zorunludur.", $"{prefix}.{nameof(area.UnitNo)}", errors);
            MaxLength(area.UnitNo, 50, "Birim No en fazla 50 karakter olabilir.", $"{prefix}.{nameof(area.UnitNo)}", errors);
            Required(area.Name, "Alan adı zorunludur.", $"{prefix}.{nameof(area.Name)}", errors);
            MaxLength(area.Name, 200, "Alan adı en fazla 200 karakter olabilir.", $"{prefix}.{nameof(area.Name)}", errors);
            if (area.Area <= 0)
                errors.Add(new ValidationError("Yüzölçümü 0'dan büyük olmalıdır.", $"{prefix}.{nameof(area.Area)}"));
            if (area.UnitTypeId is null or <= 0)
                errors.Add(new ValidationError("Alan türü zorunludur.", $"{prefix}.{nameof(area.UnitTypeId)}"));
            if (area.FreeDurationMinutes < 0)
                errors.Add(new ValidationError("Ücretsiz süre negatif olamaz.", $"{prefix}.{nameof(area.FreeDurationMinutes)}"));
            if (area.HourlyRate < 0)
                errors.Add(new ValidationError("Saatlik ücret negatif olamaz.", $"{prefix}.{nameof(area.HourlyRate)}"));
            if (area.VatRate is < 0 or > 100)
                errors.Add(new ValidationError("KDV oranı 0 ile 100 arasında olmalıdır.", $"{prefix}.{nameof(area.VatRate)}"));
        }
    }

    private static void ValidateDuplicateUnitNumbers(CreatePropertyInput input, List<ValidationError> errors)
    {
        var duplicate = input.Units.Select(unit => unit.UnitNo)
            .Concat(input.ReservationAreas.Select(area => area.UnitNo))
            .Where(number => !string.IsNullOrWhiteSpace(number))
            .GroupBy(number => number!.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicate != null)
            errors.Add(new ValidationError($"Birim No '{duplicate}' aynı taşınmaz içinde tekrar kullanılamaz.", nameof(input.Units)));
    }

    private static void ValidateDuplicateIds(CreatePropertyInput input, List<ValidationError> errors)
    {
        var ids = input.Units.Select(unit => unit.Id)
            .Concat(input.ReservationAreas.Select(area => area.Id))
            .Where(id => id.HasValue)
            .Select(id => id!.Value);
        if (ids.GroupBy(id => id).Any(group => group.Count() > 1))
            errors.Add(new ValidationError("Aynı birim kaydı birden fazla kez gönderilemez.", nameof(input.Units)));
    }

    private static void ValidatePricing(SavePropertyPricingMatrixInput matrix, List<ValidationError> errors)
    {
        var pairs = new HashSet<(int CategoryId, int ChargeTypeId)>();
        for (var rowIndex = 0; rowIndex < matrix.Rows.Count; rowIndex++)
        {
            var row = matrix.Rows[rowIndex];
            for (var cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
            {
                var cell = row.Cells[cellIndex];
                var prefix = $"PricingMatrix.Rows[{rowIndex}].Cells[{cellIndex}]";
                if (cell.TenantCategoryId <= 0 || cell.TenantCategoryId != row.TenantCategoryId || cell.ChargeTypeId <= 0)
                    errors.Add(new ValidationError("Fiyat matrisi satırı geçersizdir.", prefix));
                if (!pairs.Add((cell.TenantCategoryId, cell.ChargeTypeId)))
                    errors.Add(new ValidationError("Aynı fiyat matrisi hücresi birden fazla kez gönderilemez.", prefix));
                if (cell.UnitValue < 0)
                    errors.Add(new ValidationError("Birim değer negatif olamaz.", $"{prefix}.{nameof(cell.UnitValue)}"));
                if (!Enum.IsDefined(cell.CalculationMethod))
                    errors.Add(new ValidationError("Geçerli bir hesaplama yöntemi seçiniz.", $"{prefix}.{nameof(cell.CalculationMethod)}"));
                if (cell.VatRate is < 0 or > 100)
                    errors.Add(new ValidationError("KDV oranı 0 ile 100 arasında olmalıdır.", $"{prefix}.{nameof(cell.VatRate)}"));
            }
        }
    }

    private static void Required(string? value, string message, string field, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(new ValidationError(message, field));
    }

    private static void MaxLength(string? value, int max, string message, string field, List<ValidationError> errors)
    {
        if (value?.Length > max) errors.Add(new ValidationError(message, field));
    }
}
