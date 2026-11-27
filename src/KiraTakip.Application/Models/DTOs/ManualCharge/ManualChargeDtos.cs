using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos.ManualCharge;

public record GetManualChargesInput(
    SpaceAccessScope? Scope = null,
    string? Status = null,
    string? Relation = null,
    int? LeaseId = null);

public record GetManualChargesPageInput(
    TableQuery Query,
    SpaceAccessScope? Scope = null,
    string? Relation = null,
    int? LeaseId = null);

public record GetCancelledManualChargeCountInput(SpaceAccessScope? Scope = null);

public record GetActiveManualChargeLeasesInput(SpaceAccessScope AccessScope);

public record CreateManualChargeInput(
    int TenantId,
    int? LeaseId,
    int SpaceId,
    int ChargeTypeId,
    string Description,
    decimal Amount,
    bool IsVatApplied,
    decimal VatRate,
    DateTime DueDate,
    string? Note,
    SpaceAccessScope AccessScope);

public record CancelManualChargeInput(
    int ChargeId,
    string Reason,
    SpaceAccessScope AccessScope);

public record GetManualChargeSpacesInput(SpaceAccessScope? Scope = null);
