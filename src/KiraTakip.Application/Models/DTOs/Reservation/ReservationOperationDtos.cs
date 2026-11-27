using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.Reservation;

public record GetReservationByIdInput(
    int ReservationId,
    SpaceAccessScope AccessScope);

public record GetTenantReservationByIdInput(
    int ReservationId,
    int TenantId,
    SpaceAccessScope AccessScope);

public record GetReservationFormOptionsInput(
    SpaceAccessScope AccessScope);

public record CalculateReservationInput(
    int SpaceId,
    DateTime StartDate,
    DateTime EndDate,
    SpaceAccessScope AccessScope);

public record CreateReservationInput(
    int SpaceId,
    int TenantId,
    DateTime StartDate,
    DateTime EndDate,
    string? Description,
    SpaceAccessScope AccessScope);

public record CreateReservationRequestInput(
    int SpaceId,
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
    SpaceAccessScope AccessScope);

public record CancelReservationInput(
    int ReservationId,
    string Reason,
    SpaceAccessScope AccessScope,
    bool CanOverrideTimeRestriction = false,
    string? ActorUserId = null);

public record CancelTenantReservationInput(
    int ReservationId,
    int TenantId,
    string Reason,
    SpaceAccessScope AccessScope,
    string ActorUserId = "");

public record ApproveReservationInput(
    int ReservationId,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    SpaceAccessScope AccessScope);

public record RejectReservationInput(
    int ReservationId,
    string Reason,
    byte[] ExpectedRowVersion,
    string ActorUserId,
    SpaceAccessScope AccessScope);

public record UpdateReservationInput(
    int ReservationId,
    int SpaceId,
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
    SpaceAccessScope AccessScope);

public record TransferReservationToChargeInput(
    int ReservationId,
    SpaceAccessScope AccessScope);

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

public record ReservationSpaceContextDto(
    int SpaceId,
    string Path,
    bool IsSpaceActive,
    SpaceUsage Usage);

public record ReservationSpaceOptionDto(int Id, string Name, string Path);

public record ReservationTenantOptionDto(int Id, string DisplayName);

public record ReservationFormOptionsDto(
    List<ReservationSpaceOptionDto> Spaces,
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
    public int? ChargeTypeId { get; set; }
    public bool HasRateRule { get; set; }
    public string? ErrorMessage { get; set; }
}
