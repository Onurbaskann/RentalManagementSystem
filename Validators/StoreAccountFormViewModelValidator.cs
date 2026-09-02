using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.Constants;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class StoreAccountFormViewModelValidator : IValidator<StoreAccountFormViewModel>
{
    public ValidationResult Validate(StoreAccountFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (!PaymentProviderCodes.Supported.Contains(input.ProviderCode, StringComparer.OrdinalIgnoreCase))
            errors.Add(new ValidationError("Desteklenmeyen ödeme sağlayıcısı.", nameof(input.ProviderCode)));

        if (!CurrencyCodes.Supported.Contains(input.Currency, StringComparer.OrdinalIgnoreCase))
            errors.Add(new ValidationError("Desteklenmeyen para birimi.", nameof(input.Currency)));

        ValidateRequired(input.MerchantId, 200, nameof(input.MerchantId), "Merchant ID", errors);
        ValidateRequired(input.MerchantUser, 200, nameof(input.MerchantUser), "Merchant kullanıcı", errors);
        ValidateRequired(input.MerchantPassword, 1000, nameof(input.MerchantPassword), "Merchant parola", errors);

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }

    private static void ValidateRequired(
        string? value,
        int maxLength,
        string field,
        string label,
        ICollection<ValidationError> errors)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            errors.Add(new ValidationError($"{label} zorunludur.", field));
        else if (normalized.Length > maxLength)
            errors.Add(new ValidationError($"{label} en fazla {maxLength} karakter olabilir.", field));
    }
}
