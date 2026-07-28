using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class CreateManualChargeViewModelValidator : IValidator<CreateManualChargeViewModel>
{
    public ValidationResult Validate(CreateManualChargeViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.TenantId <= 0)
            errors.Add(new ValidationError("Kiracı seçilmelidir.", nameof(input.TenantId)));

        if (input.UnitId <= 0)
            errors.Add(new ValidationError("Birim seçilmelidir.", nameof(input.UnitId)));

        if (input.ChargeTypeId <= 0)
            errors.Add(new ValidationError("Borç tipi seçilmelidir.", nameof(input.ChargeTypeId)));

        if (string.IsNullOrWhiteSpace(input.Description))
            errors.Add(new ValidationError("Açıklama zorunludur.", nameof(input.Description)));
        else if (input.Description.Length > 200)
            errors.Add(new ValidationError("Açıklama en fazla 200 karakter olabilir.", nameof(input.Description)));

        if (input.Amount < 0.01m)
            errors.Add(new ValidationError("Tutar sıfırdan büyük olmalıdır.", nameof(input.Amount)));

        if (input.DueDate == default)
            errors.Add(new ValidationError("Vade tarihi zorunludur.", nameof(input.DueDate)));

        if (input.VatRate < 0 || input.VatRate > 100)
            errors.Add(new ValidationError("KDV oranı 0-100 arasında olmalıdır.", nameof(input.VatRate)));

        if (input.Note?.Length > 500)
            errors.Add(new ValidationError("Not en fazla 500 karakter olabilir.", nameof(input.Note)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
