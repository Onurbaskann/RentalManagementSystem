namespace KiraTakip.Models.Dtos;

public record SaveReservationRateRuleInput(
    int Id,
    int? UnitId,
    int FreeDurationMinutes,
    int BillingPeriodMinutes,
    decimal PeriodRate,
    decimal KdvRate,
    string? Description,
    bool IsActive
);

public record GetRateRuleByIdInput(int Id);

public record ToggleRateRuleStatusInput(int Id);

public record SaveUnitReservationRateRuleInput(
    int Id,
    int UnitId,
    int FreeDurationMinutes,
    int BillingPeriodMinutes,
    decimal PeriodRate,
    decimal KdvRate,
    string? Description,
    bool IsActive,
    ReservationAccessScopeInput AccessScope);

public record ClearUnitReservationRateRuleInput(
    int UnitId,
    ReservationAccessScopeInput AccessScope);
