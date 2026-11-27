namespace KiraTakip.Models.Dtos.Reservation;

public record SaveReservationRateRuleInput(
    int Id,
    int SpaceId,
    int ChargeTypeId,
    int FreeDurationMinutes,
    int BillingPeriodMinutes,
    decimal PeriodRate,
    decimal KdvRate,
    string? Description,
    bool IsActive,
    SpaceAccessScope AccessScope
);

public record GetRateRuleByIdInput(int Id, SpaceAccessScope AccessScope);

public record ToggleRateRuleStatusInput(int Id, SpaceAccessScope AccessScope);

