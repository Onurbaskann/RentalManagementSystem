using KiraTakip.Infrastructure.Validation;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public class PaymentStoreRoutingFormViewModelValidator : IValidator<PaymentStoreRoutingFormViewModel>
{
    public ValidationResult Validate(PaymentStoreRoutingFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.ChargeTypeId <= 0)
            errors.Add(new ValidationError("Borç tipi zorunludur.", nameof(input.ChargeTypeId)));
        if (input.StoreId <= 0)
            errors.Add(new ValidationError("Mağaza zorunludur.", nameof(input.StoreId)));

        if (input.SpaceId is <= 0)
            errors.Add(new ValidationError("Geçersiz alan seçimi.", nameof(input.SpaceId)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
