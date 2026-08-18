using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public class ApproveReservationViewModelValidator : IValidator<ApproveReservationViewModel>
{
    public ValidationResult Validate(ApproveReservationViewModel input)
        => input.RowVersion.Length == 0
            ? ValidationResult.Invalid(new ValidationError(
                "Rezervasyon sürüm bilgisi eksik. Sayfayı yenileyip tekrar deneyin.",
                nameof(input.RowVersion)))
            : ValidationResult.Valid();
}

public class RejectReservationViewModelValidator : IValidator<RejectReservationViewModel>
{
    public ValidationResult Validate(RejectReservationViewModel input)
    {
        var errors = new List<ValidationError>();
        if (input.RowVersion.Length == 0)
            errors.Add(new ValidationError(
                "Rezervasyon sürüm bilgisi eksik. Sayfayı yenileyip tekrar deneyin.",
                nameof(input.RowVersion)));
        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 450)
            errors.Add(new ValidationError(
                "Ret gerekçesi zorunlu ve en fazla 450 karakter olmalıdır.",
                nameof(input.Reason)));
        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
