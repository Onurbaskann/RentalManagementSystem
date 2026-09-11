using KiraTakip.Data;
using KiraTakip.Domain.Reservations;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Reservations;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Services.Reservations;

public class ReservationCompletionService(
    IReservationRepository reservationRepository,
    IReservationBusinessRules reservationBusinessRules,
    IReservationPolicyProvider reservationPolicyProvider,
    IUnitOfWork unitOfWork) : IReservationCompletionService, ITransactionalService
{
    public Task<List<int>> FindCandidatesAsync(
        FindReservationCompletionCandidatesInput input)
    {
        Guard.Against(
            input.BatchSize <= 0,
            "Rezervasyon tamamlama batch boyutu pozitif olmalıdır.",
            "RESERVATION_COMPLETION_INVALID_BATCH_SIZE");
        return reservationRepository.GetCompletionCandidateIdsAsync(
            GetCutoff(),
            input.BatchSize);
    }

    public async Task<bool> CompleteAsync(CompleteReservationInput input)
    {
        Guard.Against(
            input.ReservationId <= 0,
            "Rezervasyon bilgisi geçersizdir.",
            "RESERVATION_COMPLETION_INVALID_ID");

        await reservationRepository.AcquireCompletionLockAsync(input.ReservationId);
        var reservation = await reservationRepository.GetForCompletionAsync(input.ReservationId);
        if (reservation == null
            || !ReservationLifecycle.CanComplete(reservation.Status)
            || !ReservationSchedulePolicy.HasReachedCompletionCutoff(reservation.EndDate, GetCutoff()))
        {
            return false;
        }

        reservationBusinessRules.EnsureTransitionAllowed(
            reservation.Status,
            ReservationStatus.Completed);
        reservation.Status = ReservationStatus.Completed;
        reservation.CompletedAt = reservationBusinessRules.GetCurrentTime();
        await unitOfWork.SaveChangesAsync();
        return true;
    }

    private DateTime GetCutoff()
        => ReservationSchedulePolicy.CalculateCompletionCutoff(
            reservationBusinessRules.GetCurrentTime(),
            reservationPolicyProvider.Current.CompletionGraceMinutes);
}
