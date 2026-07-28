namespace KiraTakip.Models.Dtos;

public record GetReservationsInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetTenantReservationsInput(
    int TenantId,
    DateTime CurrentTime,
    ReservationAccessScopeInput AccessScope);
