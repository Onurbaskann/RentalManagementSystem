using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class CreatePaymentViewModelValidator : IValidator<CreatePaymentViewModel>
{
    public ValidationResult Validate(CreatePaymentViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.ChargeId <= 0)
            errors.Add(new ValidationError("Tahakkuk seçilmelidir.", nameof(input.ChargeId)));

        if (input.ChargeLineItemId is not > 0)
            errors.Add(new ValidationError(
                "Ödeme yapılacak tahakkuk kalemi seçilmelidir.",
                nameof(input.ChargeLineItemId)));

        if (input.Amount < 0.01m)
            errors.Add(new ValidationError("Tutar sıfırdan büyük olmalıdır.", nameof(input.Amount)));

        if (input.PaymentDate == default)
            errors.Add(new ValidationError("Ödeme tarihi zorunludur.", nameof(input.PaymentDate)));

        if (!Enum.IsDefined(input.PaymentChannel))
            errors.Add(new ValidationError("Geçerli bir ödeme kanalı seçilmelidir.", nameof(input.PaymentChannel)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
