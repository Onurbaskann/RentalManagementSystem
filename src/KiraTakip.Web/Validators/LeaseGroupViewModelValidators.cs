using KiraTakip.Infrastructure.Validation;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;

namespace KiraTakip.Web.Validators;

public sealed class LeaseGroupFormViewModelValidator : IValidator<LeaseGroupFormViewModel>
{
    public ValidationResult Validate(LeaseGroupFormViewModel input)
    {
        var errors = new List<ValidationError>();

        if (input.IsEdit is false && input.UnitId is null or <= 0)
            errors.Add(new ValidationError("Lütfen bir birim seçin.", nameof(input.UnitId)));
        if (input.EndDate <= input.StartDate)
            errors.Add(new ValidationError("Bitiş tarihi başlangıç tarihinden büyük olmalıdır.", nameof(input.EndDate)));
        if (input.DueDateRuleType is not (DueDateRuleType.FixedDayOfMonth or DueDateRuleType.PeriodStartOffset))
            errors.Add(new ValidationError("Geçerli bir vade kuralı seçilmelidir.", nameof(input.DueDateRuleType)));
        if (input.DueDay is < 1 or > 31)
            errors.Add(new ValidationError("Vade günü 1-31 arasında olmalıdır.", nameof(input.DueDay)));

        for (var index = 0; index < input.Members.Count; index++)
        {
            var member = input.Members[index];
            var prefix = $"{nameof(input.Members)}[{index}]";
            if (member.TenantId <= 0)
                errors.Add(new ValidationError("Lütfen bir kiracı seçin.", $"{prefix}.{nameof(member.TenantId)}"));
            if (member.Area <= 0)
                errors.Add(new ValidationError("m² sıfırdan büyük olmalıdır.", $"{prefix}.{nameof(member.Area)}"));

            LeaseLineItemValidationRules.AddErrors(
                member.LeaseLineItems
                    .Where(lineItem => !input.IsRentFree
                        || !string.Equals(lineItem.ChargeTypeCode, BorcTipiConsts.Kira, StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                $"{prefix}.{nameof(member.LeaseLineItems)}",
                errors);
        }

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}

public sealed class ApproveLeaseGroupViewModelValidator : IValidator<ApproveLeaseGroupViewModel>
{
    public ValidationResult Validate(ApproveLeaseGroupViewModel input)
        => LeaseGroupValidation.Validate(input.GroupId, input.Explanation, required: false, "Onay açıklaması");
}

public sealed class ResubmitLeaseGroupViewModelValidator : IValidator<ResubmitLeaseGroupViewModel>
{
    public ValidationResult Validate(ResubmitLeaseGroupViewModel input)
        => LeaseGroupValidation.Validate(input.GroupId, input.Explanation, required: false, "Açıklama");
}

public sealed class DeleteLeaseGroupDraftViewModelValidator : IValidator<DeleteLeaseGroupDraftViewModel>
{
    public ValidationResult Validate(DeleteLeaseGroupDraftViewModel input)
        => LeaseGroupValidation.Validate(input.GroupId, input.Explanation, required: true, "Silme açıklaması");
}

internal static class LeaseGroupValidation
{
    public static ValidationResult Validate(int groupId, string? explanation, bool required, string label)
    {
        var errors = new List<ValidationError>();
        if (groupId <= 0)
            errors.Add(new ValidationError("Sözleşme grubu bulunamadı.", "GroupId"));
        var normalized = explanation?.Trim();
        if (required && string.IsNullOrEmpty(normalized))
            errors.Add(new ValidationError($"{label} zorunludur.", "Explanation"));
        else if (normalized?.Length > 1000)
            errors.Add(new ValidationError($"{label} en fazla 1000 karakter olabilir.", "Explanation"));
        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}

public sealed class ExtendLeaseGroupViewModelValidator : IValidator<ExtendLeaseGroupViewModel>
{
    public ValidationResult Validate(ExtendLeaseGroupViewModel input)
    {
        var errors = new List<ValidationError>();
        if (input.GroupId <= 0)
            errors.Add(new ValidationError("Sözleşme grubu bulunamadı.", nameof(input.GroupId)));
        if (input.NewEndDate == default)
            errors.Add(new ValidationError("Yeni bitiş tarihi zorunludur.", nameof(input.NewEndDate)));
        if (input.ApplyInflation && input.InflationRate is < 0)
            errors.Add(new ValidationError("TÜFE oranı negatif olamaz.", nameof(input.InflationRate)));
        if (input.ApplyVat && input.VatRate is < 0 or > 100)
            errors.Add(new ValidationError("KDV oranı 0-100 arasında olmalıdır.", nameof(input.VatRate)));
        if (input.Description?.Trim().Length > 1000)
            errors.Add(new ValidationError("Açıklama en fazla 1000 karakter olabilir.", nameof(input.Description)));

        for (var index = 0; index < input.Members.Count; index++)
            LeaseLineItemValidationRules.AddErrors(
                input.Members[index].LeaseLineItems,
                $"{nameof(input.Members)}[{index}].{nameof(ExtendLeaseGroupMemberFormModel.LeaseLineItems)}",
                errors);

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}

public sealed class TerminateLeaseGroupViewModelValidator : IValidator<TerminateLeaseGroupViewModel>
{
    public ValidationResult Validate(TerminateLeaseGroupViewModel input)
    {
        var errors = new List<ValidationError>();
        if (input.GroupId <= 0)
            errors.Add(new ValidationError("Sözleşme grubu bulunamadı.", nameof(input.GroupId)));
        if (input.TerminationDate == default)
            errors.Add(new ValidationError("Fesih tarihi zorunludur.", nameof(input.TerminationDate)));
        if (string.IsNullOrWhiteSpace(input.TerminationReason))
            errors.Add(new ValidationError("Fesih nedeni zorunludur.", nameof(input.TerminationReason)));
        if (input.Description?.Trim().Length > 1000)
            errors.Add(new ValidationError("Açıklama en fazla 1000 karakter olabilir.", nameof(input.Description)));
        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}

public sealed class LeaseGroupRestructureViewModelValidator : IValidator<LeaseGroupRestructureViewModel>
{
    public ValidationResult Validate(LeaseGroupRestructureViewModel input)
    {
        var errors = new List<ValidationError>();
        if (input.GroupId <= 0)
            errors.Add(new ValidationError("Sözleşme grubu bulunamadı.", nameof(input.GroupId)));
        if (input.TerminationDate == default)
            errors.Add(new ValidationError("Fesih tarihi zorunludur.", nameof(input.TerminationDate)));
        if (string.IsNullOrWhiteSpace(input.TerminationReason))
            errors.Add(new ValidationError("Fesih nedeni zorunludur.", nameof(input.TerminationReason)));
        if (input.TerminationDescription?.Trim().Length > 1000)
            errors.Add(new ValidationError("Fesih açıklaması en fazla 1000 karakter olabilir.", nameof(input.TerminationDescription)));
        if (input.EndDate <= input.StartDate)
            errors.Add(new ValidationError("Bitiş tarihi başlangıç tarihinden büyük olmalıdır.", nameof(input.EndDate)));
        if (input.DueDateRuleType is not (DueDateRuleType.FixedDayOfMonth or DueDateRuleType.PeriodStartOffset))
            errors.Add(new ValidationError("Geçerli bir vade kuralı seçilmelidir.", nameof(input.DueDateRuleType)));
        if (input.DueDay is < 1 or > 31)
            errors.Add(new ValidationError("Vade günü 1-31 arasında olmalıdır.", nameof(input.DueDay)));

        for (var index = 0; index < input.Members.Count; index++)
        {
            var member = input.Members[index];
            var prefix = $"{nameof(input.Members)}[{index}]";
            if (member.TenantId <= 0)
                errors.Add(new ValidationError("Lütfen bir kiracı seçin.", $"{prefix}.{nameof(member.TenantId)}"));
            if (member.Area <= 0)
                errors.Add(new ValidationError("m² sıfırdan büyük olmalıdır.", $"{prefix}.{nameof(member.Area)}"));

            LeaseLineItemValidationRules.AddErrors(
                member.LeaseLineItems
                    .Where(lineItem => !input.IsRentFree
                        || !string.Equals(lineItem.ChargeTypeCode, BorcTipiConsts.Kira, StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                $"{prefix}.{nameof(member.LeaseLineItems)}",
                errors);
        }

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }
}
