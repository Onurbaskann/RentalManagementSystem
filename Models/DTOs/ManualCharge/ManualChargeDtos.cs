namespace KiraTakip.Models.Dtos;

public record GetManualChargesInput(
    IReadOnlyList<int>? PropertyIds = null,
    string? Status = null,
    string? Relation = null,
    int? LeaseId = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetCancelledManualChargeCountInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record ManualChargeAccessScopeInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetActiveManualChargeLeasesInput(ManualChargeAccessScopeInput AccessScope);

public record CreateManualChargeInput(
    int TenantId,
    int? LeaseId,
    int UnitId,
    int ChargeTypeId,
    string Description,
    decimal Amount,
    bool IsVatApplied,
    decimal VatRate,
    DateTime DueDate,
    string? Note,
    ManualChargeAccessScopeInput AccessScope);

public record CancelManualChargeInput(
    int ChargeId,
    string Reason,
    ManualChargeAccessScopeInput AccessScope);

public record GetManualChargeUnitsInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);
