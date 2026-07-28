namespace KiraTakip.Models.Dtos;

public record ReservationAccessScopeInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetReservationByIdInput(
    int ReservationId,
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

public record CancelReservationInput(
    int ReservationId,
    string Reason,
    ReservationAccessScopeInput AccessScope);

public record TransferReservationToChargeInput(
    int ReservationId,
    ReservationAccessScopeInput AccessScope);

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
