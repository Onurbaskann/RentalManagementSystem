using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class RejectPaymentViewModelValidator : IValidator<RejectPaymentViewModel>
{
    public ValidationResult Validate(RejectPaymentViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.PaymentId <= 0)
            errors.Add(new ValidationError("Ödeme kaydı bulunamadı.", nameof(input.PaymentId)));

        if (string.IsNullOrWhiteSpace(input.Reason))
            errors.Add(new ValidationError("Red nedeni zorunludur.", nameof(input.Reason)));
        else if (input.Reason.Length > 500)
            errors.Add(new ValidationError("Red nedeni en fazla 500 karakter olabilir.", nameof(input.Reason)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
