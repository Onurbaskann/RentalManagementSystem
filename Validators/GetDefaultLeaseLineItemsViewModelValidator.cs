using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class GetDefaultLeaseLineItemsViewModelValidator : IValidator<GetDefaultLeaseLineItemsViewModel>
{
    public ValidationResult Validate(GetDefaultLeaseLineItemsViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.UnitId <= 0)
            errors.Add(new ValidationError("Geçerli bir birim seçilmelidir.", nameof(input.UnitId)));
        if (input.TenantId <= 0)
            errors.Add(new ValidationError("Geçerli bir kiracı seçilmelidir.", nameof(input.TenantId)));
        if (input.StartDate == default)
            errors.Add(new ValidationError("Başlangıç tarihi zorunludur.", nameof(input.StartDate)));
        if (input.LeaseId is <= 0)
            errors.Add(new ValidationError("Geçerli bir sözleşme seçilmelidir.", nameof(input.LeaseId)));

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
