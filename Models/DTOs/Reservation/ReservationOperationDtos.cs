namespace KiraTakip.Models.Dtos;

public record ReservationAccessScopeInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetReservationByIdInput(
    int ReservationId,
    ReservationAccessScopeInput AccessScope);

public record GetTenantReservationByIdInput(
    int ReservationId,
    int TenantId,
    ReservationAccessScopeInput AccessScope);

public record GetReservationFormOptionsInput(
    ReservationAccessScopeInput AccessScope);

public record CalculateReservationInput(
    int UnitId,
    DateTime StartDate,
    DateTime EndDate,
    ReservationAccessScopeInput AccessScope);

public record CreateReservationInput(
    int UnitId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    string? Description,
    ReservationAccessScopeInput AccessScope);

public record CreateReservationRequestInput(
    int UnitId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    string Title,
    string? Description,
    string? Notes,
    string? InternalNotes,
    IReadOnlyList<ReservationAttendeePolicyInput> Attendees,
    bool CreateAndApprove,
    string RequestedByUserId,
    string RequestedByDisplayName,
    string RequestedByEmailAddress,
    ReservationAccessScopeInput AccessScope);

public record CancelReservationInput(
    int ReservationId,
    string Reason,
    ReservationAccessScopeInput AccessScope,
    bool CanOverrideTimeRestriction = false,
    string? ActorUserId = null);

public record CancelTenantReservationInput(
    int ReservationId,
    int TenantId,
    string Reason,
    ReservationAccessScopeInput AccessScope,
    string ActorUserId = "");

public record ApproveReservationInput(
    int ReservationId,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    ReservationAccessScopeInput AccessScope);

public record RejectReservationInput(
    int ReservationId,
    string Reason,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    ReservationAccessScopeInput AccessScope);

public record UpdateReservationInput(
    int ReservationId,
    int UnitId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    string Title,
    string? Description,
    string? Notes,
    string? InternalNotes,
    IReadOnlyList<ReservationAttendeePolicyInput> Attendees,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    string ActorDisplayName,
    string ActorEmailAddress,
    bool CanOverrideTimeRestriction,
    string? OverrideReason,
    ReservationAccessScopeInput AccessScope);

public record TransferReservationToChargeInput(
    int ReservationId,
    ReservationAccessScopeInput AccessScope);

public record ReservationAttendeePolicyInput(
    string? DisplayName,
    string? EmailAddress,
    bool IsReservationOwner);

public record ReservationContentPolicyInput(
    string? Title,
    string? Description,
    string? Notes,
    string? InternalNotes,
    IReadOnlyList<ReservationAttendeePolicyInput> Attendees);

public record ReservationUnitContextDto(
    int UnitId,
    int PropertyId,
    int UnitTypeId,
    string UnitTypeName,
    bool IsUnitActive,
    bool IsUnitTypeActive,
    UnitTypeUsage Usage);

public record ReservationTenantOptionDto(int Id, string DisplayName);

public record ReservationFormOptionsDto(
    List<UnitListItemDto> Units,
    List<ReservationTenantOptionDto> Tenants);

public class ReservationCalculationResultDto
{
    public int TotalDurationMinutes { get; set; }
    public int FreeDurationMinutes { get; set; }
    public int PaidDurationMinutes { get; set; }
    public int PaidPeriodCount { get; set; }
    public decimal UnitRate { get; set; }
    public decimal RateAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool HasRateRule { get; set; }
    public string? ErrorMessage { get; set; }
}
