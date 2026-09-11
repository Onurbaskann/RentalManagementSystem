using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Services.Interfaces.Reservations;

public interface IReservationCompletionService
{
    Task<List<int>> FindCandidatesAsync(FindReservationCompletionCandidatesInput input);
    Task<bool> CompleteAsync(CompleteReservationInput input);
}
