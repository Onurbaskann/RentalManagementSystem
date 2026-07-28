using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class PaymentPortalRequestViewModelValidator : IValidator<PaymentPortalRequestViewModel>
{
    public ValidationResult Validate(PaymentPortalRequestViewModel input)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(input.Token))
            errors.Add(new ValidationError("Ödeme bağlantısı zorunludur.", nameof(input.Token)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
