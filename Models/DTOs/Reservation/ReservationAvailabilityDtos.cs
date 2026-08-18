namespace KiraTakip.Models.Dtos;

public record ReservationCalendarRepositoryQuery(
    DateTime FromInclusive,
    DateTime ToExclusive,
    int? UnitId,
    IReadOnlyList<int>? PropertyIds,
    IReadOnlyList<int>? UnitIds);

public record GetReservationCalendarInput(
    DateTime? AnchorDate,
    int? UnitId,
    ReservationAccessScopeInput AccessScope);

public record GetTenantReservationCalendarInput(
    int TenantId,
    DateTime? AnchorDate,
    int? UnitId,
    ReservationAccessScopeInput AccessScope);

public record CheckReservationAvailabilityInput(
    int UnitId,
    DateTime StartDate,
    DateTime EndDate,
    int? ExcludedReservationId,
    ReservationAccessScopeInput AccessScope);

public record ReservationCalendarItemDto(
    int ReservationId,
    int UnitId,
    string UnitName,
    string PropertyName,
    string? Title,
    string TenantDisplayName,
    DateTime StartDate,
    DateTime EndDate,
    ReservationStatus Status);

public record TenantReservationCalendarItemDto(
    int UnitId,
    DateTime StartDate,
    DateTime EndDate,
    ReservationStatus Status,
    bool IsOwnedByCurrentTenant);

public record ReservationCalendarResultDto(
    DateTime FromDate,
    DateTime ToDate,
    int? SelectedUnitId,
    List<UnitListItemDto> Units,
    List<ReservationCalendarItemDto> Items);

public record TenantReservationCalendarResultDto(
    DateTime FromDate,
    DateTime ToDate,
    int? SelectedUnitId,
    List<UnitListItemDto> Units,
    List<TenantReservationCalendarItemDto> Items);

public record ReservationAvailabilityResultDto(
    bool IsAvailable,
    string Code,
    string Message);
