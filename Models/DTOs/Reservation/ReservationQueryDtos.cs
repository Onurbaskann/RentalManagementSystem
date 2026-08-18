using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos;

public record GetReservationsInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetReservationsPageInput(
    TableQuery Query,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetCancelledReservationCountInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetTenantReservationsInput(
    int TenantId,
    ReservationAccessScopeInput AccessScope);

public record GetTenantReservationsPageInput(
    int TenantId,
    TableQuery Query,
    ReservationAccessScopeInput AccessScope);
