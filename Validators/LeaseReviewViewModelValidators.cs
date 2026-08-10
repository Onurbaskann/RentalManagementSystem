using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Validators;

public sealed class LeaseDraftViewModelValidator : IValidator<LeaseDraftViewModel>
{
    public ValidationResult Validate(LeaseDraftViewModel input)
    {
        var errors = LeaseReviewValidation.ValidateIdentity(input.LeaseId, input.RowVersion);
        if (input.UnitId is null or <= 0)
            errors.Add(new ValidationError("Lütfen bir birim seçin.", nameof(input.UnitId)));
        if (input.TenantId <= 0)
            errors.Add(new ValidationError("Lütfen bir kiracı seçin.", nameof(input.TenantId)));
        if (input.EndDate <= input.StartDate)
            errors.Add(new ValidationError("Bitiş tarihi başlangıç tarihinden büyük olmalıdır.", nameof(input.EndDate)));
        if (input.DueDateRuleType is not (DueDateRuleType.FixedDayOfMonth or DueDateRuleType.PeriodStartOffset))
            errors.Add(new ValidationError("Geçerli bir vade kuralı seçilmelidir.", nameof(input.DueDateRuleType)));
        if (input.DueDay is < 1 or > 31)
            errors.Add(new ValidationError("Vade günü 1-31 arasında olmalıdır.", nameof(input.DueDay)));
        LeaseLineItemValidationRules.AddErrors(input.LeaseLineItems, nameof(input.LeaseLineItems), errors);
        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}

public sealed class RequestLeaseRevisionViewModelValidator : IValidator<RequestLeaseRevisionViewModel>
{
    public ValidationResult Validate(RequestLeaseRevisionViewModel input)
        => LeaseReviewValidation.ValidateRequiredReason(input.LeaseId, input.RowVersion, input.Reason, nameof(input.Reason), "Revizyon açıklaması");
}

public sealed class DeleteLeaseDraftViewModelValidator : IValidator<DeleteLeaseDraftViewModel>
{
    public ValidationResult Validate(DeleteLeaseDraftViewModel input)
        => LeaseReviewValidation.ValidateRequiredReason(input.LeaseId, input.RowVersion, input.Reason, nameof(input.Reason), "Silme açıklaması");
}

public sealed class ApproveLeaseViewModelValidator : IValidator<ApproveLeaseViewModel>
{
    public ValidationResult Validate(ApproveLeaseViewModel input)
    {
        var errors = LeaseReviewValidation.ValidateIdentity(input.LeaseId, input.RowVersion);
        if (input.Explanation?.Trim().Length > 1000)
            errors.Add(new ValidationError("Onay açıklaması en fazla 1000 karakter olabilir.", nameof(input.Explanation)));
        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}

internal static class LeaseReviewValidation
{
    public static ValidationResult ValidateRequiredReason(
        int leaseId,
        byte[] rowVersion,
        string? reason,
        string field,
        string label)
    {
        var errors = ValidateIdentity(leaseId, rowVersion);
        var normalized = reason?.Trim();
        if (string.IsNullOrEmpty(normalized))
            errors.Add(new ValidationError($"{label} zorunludur.", field));
        else if (normalized.Length > 1000)
            errors.Add(new ValidationError($"{label} en fazla 1000 karakter olabilir.", field));
        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }

    public static List<ValidationError> ValidateIdentity(int leaseId, byte[]? rowVersion)
    {
        var errors = new List<ValidationError>();
        if (leaseId <= 0)
            errors.Add(new ValidationError("Sözleşme başvurusu bulunamadı.", "LeaseId"));
        if (rowVersion is not { Length: > 0 })
            errors.Add(new ValidationError("Başvuru sürüm bilgisi eksik. Sayfayı yenileyip tekrar deneyin.", "RowVersion"));
        return errors;
    }
}
