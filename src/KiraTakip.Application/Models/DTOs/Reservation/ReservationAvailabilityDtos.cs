using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.Reservation;

public record ReservationCalendarRepositoryQuery(
    DateTime FromInclusive,
    DateTime ToExclusive,
    int? SpaceId,
    SpaceAccessScope Scope);

public record GetReservationCalendarInput(
    DateTime? AnchorDate,
    int? SpaceId,
    SpaceAccessScope AccessScope);

public record GetTenantReservationCalendarInput(
    int TenantId,
    DateTime? AnchorDate,
    int? SpaceId,
    SpaceAccessScope AccessScope);

public record CheckReservationAvailabilityInput(
    int SpaceId,
    DateTime StartDate,
    DateTime EndDate,
    int? ExcludedReservationId,
    SpaceAccessScope AccessScope);

public record ReservationCalendarItemDto(
    int ReservationId,
    int SpaceId,
    string SpaceName,
    string SpacePath,
    string? Title,
    string TenantDisplayName,
    DateTime StartDate,
    DateTime EndDate,
    ReservationStatus Status);

public record TenantReservationCalendarItemDto(
    int SpaceId,
    DateTime StartDate,
    DateTime EndDate,
    ReservationStatus Status,
    bool IsOwnedByCurrentTenant);

public record ReservationCalendarResultDto(
    DateTime FromDate,
    DateTime ToDate,
    int? SelectedSpaceId,
    List<ReservationSpaceOptionDto> Spaces,
    List<ReservationCalendarItemDto> Items);

public record TenantReservationCalendarResultDto(
    DateTime FromDate,
    DateTime ToDate,
    int? SelectedSpaceId,
    List<ReservationSpaceOptionDto> Spaces,
    List<TenantReservationCalendarItemDto> Items);

public record ReservationAvailabilityResultDto(
    bool IsAvailable,
    string Code,
    string Message);
