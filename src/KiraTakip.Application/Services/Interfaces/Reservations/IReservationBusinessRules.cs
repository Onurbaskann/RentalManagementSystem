using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Services.Interfaces.Reservations;

public interface IReservationBusinessRules : IBusinessRules
{
    DateTime GetCurrentTime();
    void EnsureScheduleIsValid(DateTime startDate, DateTime endDate, bool validatePastDate = true);
    void EnsureContentIsValid(ReservationContentPolicyInput input);
    void EnsureUnitIsReservable(ReservationUnitContextDto unit, bool fieldValidation = false);
    void EnsureAccessScope(int propertyId, int unitId, ReservationAccessScopeInput accessScope);
    void EnsureTransitionAllowed(ReservationStatus currentStatus, ReservationStatus targetStatus);
    void EnsureCancellationAllowed(Reservation reservation, bool canOverrideTimeRestriction);
    void EnsureModificationAllowed(
        Reservation reservation,
        bool canOverrideTimeRestriction,
        string? overrideReason);
    void EnsureTenantOwnership(int reservationTenantId, int currentTenantId);
}
