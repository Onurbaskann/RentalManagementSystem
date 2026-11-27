using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos.Reservation;

public record GetReservationsInput(
    SpaceAccessScope Scope);

public record GetReservationsPageInput(
    TableQuery Query,
    SpaceAccessScope Scope);

public record GetCancelledReservationCountInput(
    SpaceAccessScope Scope);

public record GetTenantReservationsInput(
    int TenantId,
    SpaceAccessScope AccessScope);

public record GetTenantReservationsPageInput(
    int TenantId,
    TableQuery Query,
    SpaceAccessScope AccessScope);
