using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class TenantChargePaymentFormViewModelValidator
    : IValidator<TenantChargePaymentFormViewModel>
{
    public ValidationResult Validate(TenantChargePaymentFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.ChargeId <= 0)
            errors.Add(new ValidationError(
                "Tahakkuk seçilmelidir.",
                nameof(input.ChargeId)));
        if (input.ChargeLineItemId is not > 0)
            errors.Add(new ValidationError(
                "Ödeme yapılacak tahakkuk kalemi seçilmelidir.",
                nameof(input.ChargeLineItemId)));
        if (input.Amount < 0.01m)
            errors.Add(new ValidationError(
                "Tutar 0'dan büyük olmalıdır.",
                nameof(input.Amount)));
        if (input.PaymentDate == default)
            errors.Add(new ValidationError(
                "Ödeme tarihi zorunludur.",
                nameof(input.PaymentDate)));
        if (!Enum.IsDefined(input.PaymentChannel))
            errors.Add(new ValidationError(
                "Geçerli bir ödeme kanalı seçilmelidir.",
                nameof(input.PaymentChannel)));

        if (input.Receipt == null || input.Receipt.Length == 0)
        {
            errors.Add(new ValidationError(
                "Dekont yüklemeniz zorunludur.",
                nameof(input.Receipt)));
        }
        else
        {
            if (Path.GetFileName(input.Receipt.FileName).Length > 255)
                errors.Add(new ValidationError(
                    "Dosya adı en fazla 255 karakter olabilir.",
                    nameof(input.Receipt)));
            if (input.Receipt.ContentType.Length > 100)
                errors.Add(new ValidationError(
                    "Dosya içerik türü en fazla 100 karakter olabilir.",
                    nameof(input.Receipt)));
        }

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors);
    }
}
