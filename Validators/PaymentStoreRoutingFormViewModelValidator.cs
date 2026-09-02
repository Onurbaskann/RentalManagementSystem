using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class PaymentStoreRoutingFormViewModelValidator : IValidator<PaymentStoreRoutingFormViewModel>
{
    public ValidationResult Validate(PaymentStoreRoutingFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.ChargeTypeId <= 0)
            errors.Add(new ValidationError("Borç tipi zorunludur.", nameof(input.ChargeTypeId)));
        if (input.StoreId <= 0)
            errors.Add(new ValidationError("Mağaza zorunludur.", nameof(input.StoreId)));

        if (!Enum.IsDefined(input.Scope))
            errors.Add(new ValidationError("Geçersiz yönlendirme kapsamı.", nameof(input.Scope)));
        else
        {
            switch (input.Scope)
            {
                case PaymentRoutingScope.General when input.PropertyId.HasValue || input.UnitId.HasValue:
                    errors.Add(new ValidationError("Genel kapsamda taşınmaz veya birim seçilemez.", nameof(input.Scope)));
                    break;
                case PaymentRoutingScope.Property when input.PropertyId is null or <= 0 || input.UnitId.HasValue:
                    errors.Add(new ValidationError("Taşınmaz kapsamında yalnız taşınmaz seçilmelidir.", nameof(input.PropertyId)));
                    break;
                case PaymentRoutingScope.Unit when input.UnitId is null or <= 0 || input.PropertyId.HasValue:
                    errors.Add(new ValidationError("Birim kapsamında yalnız birim seçilmelidir.", nameof(input.UnitId)));
                    break;
            }
        }

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
